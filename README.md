# Personal Coach

A personal coach web app, for single-user use on the home LAN, hosting four coach
personas (Career, Health, Relationships, Kids) that track goals, action items, and
reflections in chat, grounded in a shared values profile, Google Calendar, and (for
Health) Garmin data. See issue #1 for the full PRD.

End-to-end chat with the Career coach is in place (issue #3): a Coach Persona
Registry (Career only for now), a Coach Context Builder (recent message window),
a Coach Conversation Engine calling an Azure AI Foundry GPT-4o deployment, and a
Blazor Server chat page, with chat history persisted in Postgres via EF Core.

## Project layout

Clean Architecture layering, with each project's internals organized by type
(Entities/, Interfaces/, Services/, ...) rather than by feature:

```
src/
  Coach.Domain/          Entities (Coach, ChatMessage, Goal, ActionItem, Reflection)
                          and enums. No dependencies.
  Coach.Application/     Interfaces (ports), Services (Persona Registry, Context
                          Builder, Conversation Engine), Models. Depends on Domain.
  Coach.Infrastructure/   EF Core Postgres persistence + the Azure OpenAI adapter
                          (implements Application's ports). Depends on Application.
  Coach.Infrastructure.GoogleCalendar/
                          Read-only Google Calendar adapter (implements
                          Application's ICalendarReader). Standalone so the
                          Google.Apis dependency stays out of Coach.Infrastructure.
  Coach.Infrastructure.Garmin/
                          Garmin Connect adapter (implements Application's
                          IGarminMetricsReader). Standalone so the
                          Unofficial.Garmin.Connect dependency stays out of
                          Coach.Infrastructure.
  Coach.GarminIngestion/  Standalone console app run as a daily k8s CronJob: logs
                          into Garmin Connect, writes a daily metric record to the
                          coach Postgres, exits. Isolated from Coach.Web so a broken
                          Garmin login can't degrade chat. See
                          docs/garmin-ingestion-setup.md.
  Coach.Web/              Blazor Server UI + composition root (Program.cs). Depends
                          on Application + Infrastructure + Infrastructure.GoogleCalendar.
tests/
  Coach.Application.Tests/   xUnit tests for the Persona Registry, Context Builder,
                             Goal Tracking, Reflections, and prompt formatting (no
                             DB or model call needed).
  Coach.Infrastructure.GoogleCalendar.Tests/
                             xUnit tests for the calendar adapter's date-range
                             handling and Google response normalization, against a
                             faked API client (no network).
  Coach.Infrastructure.Garmin.Tests/
                             xUnit tests for the Garmin adapter's response-shape
                             mapping into the daily-metric record and the
                             collector's config/error handling, against a faked
                             Garmin client (no network).
  Coach.GarminIngestion.Tests/
                             xUnit tests for the ingestion job's day window and
                             its exit code on a Garmin failure, against faked
                             reader/store.
scripts/
  build-web-assets.ps1   Local-dev helper: builds Tailwind CSS + vendors Alpine.js
                          into wwwroot so `dotnet run` works without Docker.
```

## Getting started

Requires the .NET 10 SDK.

```powershell
./scripts/build-web-assets.ps1   # one-time, until wwwroot's generated assets change
dotnet run --project src/Coach.Web
```

Configure `ConnectionStrings:CoachDb` and the `AzureAi:Endpoint` / `AzureAi:ApiKey` /
`AzureAi:DeploymentName` settings (e.g. via `dotnet user-secrets` or the matching
`ConnectionStrings__CoachDb` / `AzureAi__*` environment variables) for the chat page
to actually reach Postgres and the model. Without them, the app still starts and the
chat page surfaces the resulting failure inline rather than crashing.

Optionally configure `GoogleCalendar:ClientId` / `GoogleCalendar:ClientSecret` /
`GoogleCalendar:RefreshToken` to fold the user's upcoming (read-only) calendar
events into every coach's context — see `docs/google-calendar-setup.md` for the
one-time OAuth steps. Without them the calendar slice is simply empty.

The Health coach also sees the latest daily Garmin metrics, populated by the
separate `Coach.GarminIngestion` CronJob (`Garmin:Email` / `Garmin:Password`,
sharing `ConnectionStrings:CoachDb`). Run it locally with
`dotnet run --project src/Coach.GarminIngestion`. See
`docs/garmin-ingestion-setup.md`.

EF Core migrations apply automatically at startup (best-effort — a failure is logged,
not fatal). To add a new migration:

```powershell
dotnet ef migrations add <Name> --project src/Coach.Infrastructure --startup-project src/Coach.Web -o Migrations
```

Run the unit tests with:

```powershell
dotnet test
```

## Deploying

Two images, both built with the repo root as the Docker build context (they
reference sibling projects under `src/`):

- `coach-web` from `src/Coach.Web/Dockerfile` (`docker build -f src/Coach.Web/Dockerfile .`).
  Tailwind CSS is compiled via the standalone CLI (no Node.js).
- `coach-garmin-ingestion` from `src/Coach.GarminIngestion/Dockerfile`
  (`docker build -f src/Coach.GarminIngestion/Dockerfile .`) — the daily Garmin
  ingestion CronJob.

Both deploy to the `coach` namespace on the home k3s cluster via the
`04-coach-platform` Terraform module and `upgrade-coach.sh` script in the sibling
`home-server` repo — that script's `docker build` invocations need to match the
repo-root build context above.
