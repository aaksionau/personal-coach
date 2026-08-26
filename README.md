# Personal Coach

A personal coach web app, for single-user use on the home LAN, hosting four coach
personas (Career, Health, Relationships, Kids) that track goals, action items, and
reflections in chat, grounded in a shared values profile, Google Calendar, and (for
Health) Garmin data. See issue #1 for the full PRD.

This is currently a deployment skeleton (issue #2): a reachable Blazor Server app
connected to its own Postgres instance, with no coach logic yet.

## Project layout

```
src/
  Coach.Web/     Blazor Server app (UI + eventually domain logic + EF Core)
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

Without a `ConnectionStrings:CoachDb` configured (e.g. via `dotnet user-secrets` or
`ConnectionStrings__CoachDb`), the home page still loads and reports the Postgres
health check as failed rather than crashing.

## Deploying

Images are built from `src/Coach.Web/Dockerfile` (Tailwind CSS compiled via the
standalone CLI, no Node.js) and deployed to the `coach` namespace on the home k3s
cluster via the `04-coach-platform` Terraform module and `upgrade-coach.sh` script in
the sibling `home-server` repo.
