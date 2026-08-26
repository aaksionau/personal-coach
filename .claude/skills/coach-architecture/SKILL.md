---
name: coach-architecture
description: Explains this repo's layering (Domain/Application/Infrastructure/Web), DI wiring, persistence conventions, and testing conventions. Use when adding a new module/entity/service to personal-coach, wiring a new dependency, writing tests, or orienting in this codebase for the first time.
---

# personal-coach architecture

Ports & adapters, four projects under `src/`, mirrored by one test project per source project under `tests/`.

## Layering (dependency direction only ever points down this list)

1. **`Coach.Domain`** — entities and enums only. No framework references, no logic beyond a `static Create(...)` factory per entity. Entities are `sealed class` with `required ... { get; init; }` props (immutable) unless a field is genuinely mutated post-creation, in which case only that prop gets `{ get; set; }`. No navigation properties between entities — relations are flat FK fields (e.g. `ChatMessage.CoachSlug`, a string, not a `Coach` reference). Composed views (an entity plus its related children) are built in the Application layer as a `record`, not via EF navigation.
2. **`Coach.Application`** — the ports-and-orchestration layer.
   - `Interfaces/` — the ports (e.g. `IChatMessageStore`, `IChatCompletionClient`). Only add an interface when a second implementation or a real test seam exists (a fake in tests counts).
   - `Models/` — records composing entities for a specific use (`CoachContext`, `ConversationTurn`).
   - `Services/` — orchestration and domain logic that's worth unit-testing against a fake port (`CoachContextBuilder`, `CoachConversationEngine`, `CoachPersonaRegistry`). A service is the thing tests exercise; the store/port behind it is swapped for a fake.
   - Never references EF Core, Npgsql, or the Azure OpenAI SDK directly.
3. **`Coach.Infrastructure`** — the adapters. EF Core (`Persistence/`, one `CoachDbContext`, Npgsql/Postgres) and the Azure AI Foundry client (`Ai/`) live here, each implementing an `Application.Interfaces` port. Reads use `.AsNoTracking()`; DbSet entity config lives in `CoachDbContext.OnModelCreating`, one `modelBuilder.Entity<T>(...)` block per entity.
4. **`Coach.Web`** — Blazor Server (interactive server render mode), Tailwind CSS (compiled via standalone CLI, no Node/npm), Alpine.js only for interactivity that doesn't warrant a Blazor round-trip. Pages inject Application services/ports directly via `@inject` — there is no separate web-facing service layer. See the `coach-ui-design` skill for the actual visual/interaction conventions.

## Wiring

Each of `Coach.Application` and `Coach.Infrastructure` exposes one `DependencyInjection.cs` with an `AddCoachXxx(...)` extension method registering everything in that layer. `Coach.Web/Program.cs` calls both. Follow existing lifetime choices: `CoachPersonaRegistry` is a `Singleton` (static in-memory data); stores/DbContext/services that touch a request's `DbContext` are `Scoped`; the chat model client is a `Singleton` (its own `ChatClient` is stateless/thread-safe).

Config-driven infra (`AzureAiOptions`, the Postgres connection string) falls back to an "unconfigured" placeholder rather than throwing at startup, so the app still boots locally without secrets — failures surface when the feature is actually used, not at process start.

## Persistence conventions

- Migrations via `dotnet ef migrations add <Name> --project src/Coach.Infrastructure --startup-project src/Coach.Web`.
- Seed data (e.g. the `Coach` rows) is derived from a single source of truth in code (`CoachPersonaRegistry.GetAll()`) rather than duplicated into `HasData(...)` literals.
- Entity aliasing: when a type name collides with a namespace/BCL type (`Coach` the entity vs. the app name), alias the `using` (`using CoachEntity = Coach.Domain.Entities.Coach;`) rather than renaming the entity.

## Testing conventions

- xUnit, `net10.0`, one test project per source project, `ProjectReference`'d directly (no test-only NuGet feed).
- `tests/<Project>.Tests/Fakes/` holds hand-rolled fakes implementing the `Application.Interfaces` ports (see `FakeChatMessageStore`) — no mocking library. A fake records what it was called with (e.g. `LastRequestedCoachSlug`) so tests can assert on interaction, not just return values.
- Tests target a service's public interface/observable behavior, not its internals — so refactoring the internals doesn't require rewriting the tests.
- Some modules are intentionally untested by design where the PRD says so (e.g. the Conversation Engine's actual model-calling glue) — check `docs/agents/` and the originating issue before assuming a missing test is an oversight.

## Adding a new domain concept, end to end

1. Entity + enum(s) in `Coach.Domain/Entities` / `Enums`.
2. Port in `Coach.Application/Interfaces`, composed view record (if needed) in `Models`, orchestration/validation logic in a new `Services/*Service`.
3. EF adapter in `Coach.Infrastructure/Persistence`, `DbSet` + `OnModelCreating` block on `CoachDbContext`, then a migration.
4. Register the port/service in the relevant `DependencyInjection.cs`.
5. Consume from `Coach.Web` via `@inject`, or from another `Application.Services` class via constructor injection.
6. Tests in `tests/Coach.Application.Tests` against the new service, using a new fake in `Fakes/` for its port.

Keep this file in sync when a new project, layer, or cross-cutting convention (not a one-off feature) lands.
