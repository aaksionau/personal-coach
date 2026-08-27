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
  Coach.Web/              Blazor Server UI + composition root (Program.cs). Depends
                          on Application + Infrastructure.
tests/
  Coach.Application.Tests/   xUnit tests for the Persona Registry, Context Builder,
                             Goal Tracking, and Reflections (no DB or model call
                             needed).
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

EF Core migrations apply automatically at startup (best-effort — a failure is logged,
not fatal). To add a new migration:

```powershell
dotnet ef migrations add <Name> --project src/Coach.Infrastructure --startup-project src/Coach.Web -o Migrations
```

Run the Application-layer unit tests with:

```powershell
dotnet test tests/Coach.Application.Tests
```

## Deploying

Images are built from `src/Coach.Web/Dockerfile` with the repo root as the Docker
build context (`docker build -f src/Coach.Web/Dockerfile .`), since the app now
references sibling projects under `src/`. Tailwind CSS is compiled via the standalone
CLI (no Node.js). The image deploys to the `coach` namespace on the home k3s cluster
via the `04-coach-platform` Terraform module and `upgrade-coach.sh` script in the
sibling `home-server` repo — that script's `docker build` invocation needs to match
the repo-root build context above.
