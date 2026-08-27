# Garmin ingestion setup

`Coach.GarminIngestion` is a standalone console app deployed as a **daily
Kubernetes CronJob**. Each run logs into Garmin Connect, pulls the last few days'
summaries — steps, sleep, resting heart rate, body battery, stress, intensity
minutes, and workouts — and upserts them into the coach Postgres.
The Health coach reads the latest row as part of its context. The main app never
calls Garmin; a broken login or a Garmin API change fails the CronJob only, not
chat.

## How it works

- `Coach.Infrastructure.Garmin` wraps the [`Unofficial.Garmin.Connect`](https://www.nuget.org/packages/Unofficial.Garmin.Connect)
  client behind the narrow `IGarminApi` seam; `GarminDailyMetricMapper` normalizes
  the response into the app's `GarminDailyMetric` + `GarminActivitySummary` shape.
- The job pulls the **last three days** each run (`today - 2 … today`, UTC).
  Garmin keys its summaries by the account's *local* calendar date, which can sit a
  day either side of the UTC date, so the window always covers the last complete
  local day (the model's grounding) plus today (something for a same-day chat) with
  a day of slack that also lets a missed run catch up.
- Writes are a **per-day replace** inside a transaction, so re-running the job — or
  catching up after a missed run — is idempotent, and a crash mid-write never leaves
  a half-replaced day.
- The composition root applies EF migrations best-effort at startup, like
  `Coach.Web` (a failure is logged, not fatal); a genuinely broken schema still
  surfaces when the ingestion below fails and the job exits `1`.
- Exit code `0` on success, `1` on any failure (→ the k8s Job is marked failed;
  `Coach.Web` is unaffected).

## Prerequisites

- A personal Garmin Connect account **with MFA disabled** — that's what makes an
  unattended daily login viable. If MFA is ever enabled on the account, this job
  needs revisiting (disable MFA, or move to an interactive-login-plus-manual-token
  model).

## Config

Bound from the `Garmin` section (`src/Coach.Infrastructure.Garmin/GarminOptions.cs`),
plus the shared `ConnectionStrings:CoachDb`:

| Setting                     | Value                          |
| --------------------------- | ------------------------------ |
| `Garmin:Email`              | Garmin Connect account email   |
| `Garmin:Password`           | Garmin Connect account password |
| `ConnectionStrings:CoachDb` | same coach Postgres as `Coach.Web` |

**Local dev** (env vars — the job has no `launchSettings.json`, so user-secrets
would also need `DOTNET_ENVIRONMENT=Development`):

```powershell
$env:Garmin__Email = "..."
$env:Garmin__Password = "..."
$env:ConnectionStrings__CoachDb = "Host=localhost;Database=coach;Username=coach;Password=..."
dotnet run --project src/Coach.GarminIngestion
```

**Production:** the credentials live in a Kubernetes secret in the `coach`
namespace, projected as `Garmin__Email` / `Garmin__Password` env vars;
`ConnectionStrings__CoachDb` reuses the existing `postgres-credentials` secret.
Both are wired by the `04-coach-platform` Terraform module in the sibling
`home-server` repo (`garmin.tf` + the `garmin_ingestion` CronJob in `apps.tf`),
and the image is built/pushed by `scripts/upgrade-coach.sh` alongside `coach-web`.

## Notes

- The SDK caches a session token in memory for the process lifetime; since each
  CronJob run is a fresh process, every run performs a full username/password
  login.
- Read-only in practice: the job only calls Garmin's `GetUserSummary`,
  `GetWellnessSleepData`, and `GetActivitiesByDate`.
