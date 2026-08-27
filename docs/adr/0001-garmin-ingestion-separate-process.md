# 1. Garmin ingestion runs as a separate process

Date: 2026-08-27

## Status

Accepted

## Context

The Health coach needs the user's daily Garmin metrics (steps, sleep, resting HR,
body battery, stress, workouts). Garmin has no official personal API; the viable
option is the community `Unofficial.Garmin.Connect` client with username/password
login. That client is unofficial, can break when Garmin changes its web endpoints,
and performs a real login on every process start. The account has no MFA, which is
what makes an unattended daily login possible at all.

## Decision

- **Separate deployable.** A standalone console app (`Coach.GarminIngestion`) run
  as a daily Kubernetes CronJob, not a hosted service inside `Coach.Web`. A bad
  login or an API-shape change fails the Job; chat with any coach is untouched.
  Mirrors the `stocks-pipeline` CronJob pattern in the sibling homelab repo.
- **SDK isolated in its own adapter project** (`Coach.Infrastructure.Garmin`),
  same as `Coach.Infrastructure.GoogleCalendar` — the unofficial dependency never
  enters `Coach.Infrastructure`.
- **Reuse `CoachDbContext`.** The CronJob references `Coach.Infrastructure` and
  writes through a new `IGarminMetricsStore`, sharing the entities and the EF
  migration pipeline. The job's image transitively carries the Azure OpenAI SDK it
  never invokes — accepted so there is a single source of schema truth.
- **Per-day idempotent upsert.** The job replaces a day's row (and its activity
  children) wholesale, so re-runs and catch-up runs are safe. It pulls yesterday
  and today each run.
- **The reader may throw.** Unlike `ICalendarReader` (which swallows failures so a
  coach turn is never blocked), `IGarminMetricsReader` surfaces failures — the
  CronJob *wants* a loud failure it can see in Job history.
- **The main app never calls Garmin.** The Health coach only reads the ingested
  table.

## Consequences

- If MFA is ever enabled on the Garmin account, the unattended job stops working
  and must move to an interactive-login-plus-stored-token model.
- Garmin data can be up to ~24h stale between runs; acceptable for coaching.
- The k8s CronJob, its schedule, and the credential Secret live in the
  `home-server` repo, consistent with how `coach-web` is deployed.
