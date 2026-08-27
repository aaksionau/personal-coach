---
name: coach-architecture
description: Explains this repo's layering (Domain/Application/Infrastructure/Web), DI wiring, persistence conventions, and testing conventions. Use when adding a new module/entity/service to personal-coach, wiring a new dependency, writing tests, or orienting in this codebase for the first time.
---

# personal-coach architecture

Ports & adapters, projects under `src/`, mirrored by one test project per source project under `tests/` (the four core projects, the per-external-API adapter projects like `Coach.Infrastructure.GoogleCalendar`, and `Coach.Web.Tests` — which covers the pure pieces of `Coach.Web`'s background work, e.g. `DigestSchedule`, not the Blazor UI).

## Layering (dependency direction only ever points down this list)

1. **`Coach.Domain`** — entities and enums only. No framework references, no logic beyond a `static Create(...)` factory per entity. Entities are `sealed class` with `required ... { get; init; }` props (immutable) unless a field is genuinely mutated post-creation, in which case only that prop gets `{ get; set; }`. No navigation properties between entities — relations are flat FK fields (e.g. `ChatMessage.CoachSlug`, a string, not a `Coach` reference). Composed views (an entity plus its related children) are built in the Application layer as a `record`, not via EF navigation.
2. **`Coach.Application`** — the ports-and-orchestration layer.
   - `Interfaces/` — the ports (e.g. `IChatMessageStore`, `IGoalStore`). Only add an interface when a second implementation or a real test seam exists (a fake in tests counts). The model deployment is the one exception: there's no `IChatCompletionClient` port — `CoachConversationEngine` takes an `AIAgent` (Microsoft Agent Framework, `Microsoft.Agents.AI`/`Microsoft.Extensions.AI`) directly, since those are themselves provider-agnostic abstractions and the model-calling glue is intentionally untested (see Testing conventions).
   - `Models/` — records composing entities for a specific use (`CoachContext`, `ConversationTurn`).
   - The rest of the layer's classes are split by kind into sibling folders, each its own `Coach.Application.<Folder>` namespace:
     - `Services/` — orchestration and domain logic over a port, worth unit-testing against a fake (`GoalTrackingService`, `ReflectionService`, `ValuesProfileService`, `CoachPersonaRegistry`). A service is the thing tests exercise; the store/port behind it is swapped for a fake.
     - `Builders/` — assemble a `Models/` record from several services without a model call (`CoachContextBuilder`); kept separate from the agents so the assembly is testable in isolation.
     - `Agents/` — the model-calling drivers (`CoachConversationEngine`, `ValuesWizardService`, `WeeklyDigestService`): build context, call the `AIAgent`, persist. The model-calling glue here is intentionally untested (see Testing conventions). `WeeklyDigestService` is the weekly cross-coach check-in text — it leans on `CoachContextBuilder.BuildAllTrackedStatesAsync` (all four personas at once) and `WeeklyDigestPromptComposer`, both of which *are* tested.
     - `Formatters/` — pure `static` functions rendering a slice of context into system-prompt text (`GoalContextFormatter`, `ReflectionContextFormatter`, `ValuesContextFormatter`, `CrossCoachContextFormatter`, `CalendarContextFormatter`, `GarminContextFormatter`, `CoachSystemPromptComposer` which stitches them together for a turn, and `WeeklyDigestPromptComposer` which does the same for the weekly digest).
     - `Tools/` — model tools (`GoalActionTools`, `ReflectionTools`, `ValuesProfileTools`) plus the shared `ModelToolGuard`. Each tool is a plain typed method wrapped with `AIFunctionFactory.Create(...)` — the JSON schema the model sees is reflected from the method's parameters (plus `[Description]` attributes), not hand-written, and tool-call arguments arrive already bound to those typed parameters instead of raw JSON.
   - Never references EF Core, Npgsql, or a concrete model-provider SDK (Azure OpenAI, OpenAI) directly.
3. **`Coach.Infrastructure`** — the adapters, mostly EF Core (Npgsql/Postgres). The layer is flat — no `Persistence/` wrapper folder — split by kind:
   - `CoachDbContext` sits at the project root (namespace `Coach.Infrastructure`). Reads use `.AsNoTracking()`; DbSet entity config lives in `CoachDbContext.OnModelCreating`, one `modelBuilder.Entity<T>(...)` block per entity.
   - `Stores/` — the `Application.Interfaces` port implementations, one `*Store` per port (namespace `Coach.Infrastructure.Stores`).
   - `Extensions/` — shared query helpers (`RecentQueryExtensions.ToRecentWindowAsync`, the "recent window" shape reused by the chat-message and reflection readers).
   - `Migrations/` — EF migrations + model snapshot (namespace `Coach.Infrastructure.Migrations`).
   - `Options/` — config POCOs bound via `services.Configure<T>(...)` (`AzureAiOptions`, section `"AzureAi"`).
   - The Azure OpenAI wiring has no client class of its own: `DependencyInjection` builds and registers the `AIAgent` singleton Application consumes directly (`AzureOpenAIClient.GetChatClient(...).AsIChatClient().AsAIAgent(...)`, from `Microsoft.Extensions.AI.OpenAI` + `Microsoft.Agents.AI`) — no persona instructions or tools are baked in at agent-creation time, since both vary per turn and are supplied on each `RunAsync` call.
3b. **`Coach.Infrastructure.GoogleCalendar`** / **`Coach.Infrastructure.Garmin`** / **`Coach.Infrastructure.Sms`** (and future `Coach.Infrastructure.<ExternalApi>` siblings) — a standalone adapter project per third-party integration. The original driver was keeping a heavy/vulnerable dependency (`Google.Apis.*`, `Unofficial.Garmin.Connect`) out of `Coach.Infrastructure`; `Coach.Infrastructure.Sms` follows the same shape for consistency even though its transport (`System.Net.Mail`) is in the BCL. Each references `Coach.Application` + `Coach.Domain` only, flat layout, its own `DependencyInjection.AddCoachXxx(config)`. Pattern inside: a `public`-surface-free adapter — only the port impl (`internal GoogleCalendarReader : ICalendarReader`, `internal SmtpSmsNotifier : ISmsNotifier`, …) and the `Add...` method are reachable; an `internal` seam interface (`IGoogleCalendarEvents`, `ISmtpEmailSender`) wraps the raw call so the adapter's own logic and a pure mapper/composer (`GoogleCalendarEventMapper`, `SmsMessageComposer`) are unit-tested against a fake with `[InternalsVisibleTo]` for the test project. Failure posture differs by purpose (see the port's own docs): `ICalendarReader` **swallows** failures so a coach turn is never blocked; `IGarminMetricsReader` and `ISmsNotifier` **throw** so an isolated scheduled job (the Garmin CronJob, the weekly digest scheduler) fails loudly.
4. **`Coach.Web`** — Blazor Server (interactive server render mode), Tailwind CSS (compiled via standalone CLI, no Node/npm), Alpine.js only for interactivity that doesn't warrant a Blazor round-trip. Pages inject Application services/ports directly via `@inject` — there is no separate web-facing service layer. Also hosts background work that doesn't warrant its own deployable: `BackgroundServices/` holds `WeeklyDigestScheduler` (a hosted `BackgroundService` that fires the weekly check-in text on a `Digest`-configured day/time, catching and logging a failed run), its `DigestOptions`, and `DigestSchedule` (the pure next-fire day/time/DST-gap arithmetic, split out and unit-tested in `Coach.Web.Tests` so the scheduler itself stays a thin delay loop). See the `coach-ui-design` skill for the actual visual/interaction conventions.

## Wiring

Each of `Coach.Application`, `Coach.Infrastructure`, `Coach.Infrastructure.GoogleCalendar`, `Coach.Infrastructure.Garmin`, and `Coach.Infrastructure.Sms` exposes one `DependencyInjection.cs` with an `AddCoachXxx(...)` extension method registering everything in that project. `Coach.Web/Program.cs` calls each (bar the Garmin one, which the ingestion CronJob calls), then adds its own hosted services and options. Follow existing lifetime choices: `CoachPersonaRegistry` is a `Singleton` (static in-memory data); stores/DbContext/services that touch a request's `DbContext` are `Scoped`; the `AIAgent` is a `Singleton` (stateless/thread-safe, like the underlying `ChatClient`).

Config-driven infra (`AzureAiOptions`, `GoogleCalendarOptions`, the Postgres connection string) falls back to an "unconfigured" placeholder rather than throwing at startup, so the app still boots locally without secrets — failures surface when the feature is actually used, not at process start (the calendar reader goes further and stays silent, returning no events).

## Persistence conventions

- Migrations via `dotnet ef migrations add <Name> --project src/Coach.Infrastructure --startup-project src/Coach.Web`.
- Seed data (e.g. the `Coach` rows) is derived from a single source of truth in code (`CoachPersonaRegistry.GetAll()`) rather than duplicated into `HasData(...)` literals.
- Entity aliasing: when a type name collides with a namespace/BCL type (`Coach` the entity vs. the app name), alias the `using` (`using CoachEntity = Coach.Domain.Entities.Coach;`) rather than renaming the entity.
- Most entities carry a flat `CoachSlug` FK. `ValuesProfile` is the exception — it's a single global row (no `CoachSlug`, no FK), read fresh per turn by `CoachContextBuilder` and injected into every persona's context. It uses a fixed primary key (`ValuesProfile.SingletonId`) that every read and write goes through, so a racing double-insert collides on the PK instead of duplicating; `ValuesProfileService.SaveProfileAsync` is the one upsert path, shared by the guided wizard tool and the direct-edit page.

## Testing conventions

- xUnit, `net10.0`, one test project per source project, `ProjectReference`'d directly (no test-only NuGet feed).
- `tests/<Project>.Tests/Fakes/` holds hand-rolled fakes implementing the `Application.Interfaces` ports (see `FakeChatMessageStore`) — no mocking library. A fake records what it was called with (e.g. `LastRequestedCoachSlug`) so tests can assert on interaction, not just return values.
- Tests target a service's public interface/observable behavior, not its internals — so refactoring the internals doesn't require rewriting the tests.
- Some modules are intentionally untested by design where the PRD says so (e.g. the Conversation Engine's actual model-calling glue) — check `docs/agents/` and the originating issue before assuming a missing test is an oversight.

## Adding a new domain concept, end to end

1. Entity + enum(s) in `Coach.Domain/Entities` / `Enums`.
2. Port in `Coach.Application/Interfaces`, composed view record (if needed) in `Models`, orchestration/validation logic in a new `Services/*Service` (or the matching `Builders`/`Agents`/`Formatters`/`Tools` folder if that's the kind of class it is).
3. EF adapter in `Coach.Infrastructure/Stores`, `DbSet` + `OnModelCreating` block on `CoachDbContext`, then a migration.
4. Register the port/service in the relevant `DependencyInjection.cs`.
5. Consume from `Coach.Web` via `@inject`, or from another `Coach.Application` class via constructor injection.
6. Tests in `tests/Coach.Application.Tests` against the new service, using a new fake in `Fakes/` for its port.

Keep this file in sync when a new project, layer, or cross-cutting convention (not a one-off feature) lands.
