---
name: coach-ui-design
description: Design system and Tailwind/Alpine.js conventions for personal-coach's Blazor Server UI (mobile-first, card-based, minimal JS). Use when building or restyling a page/component in Coach.Web, adding client-side interactivity, or deciding whether something needs Alpine.js.
---

# personal-coach UI design

Tailwind CSS (utility classes only, no component library) + Alpine.js used sparingly for interactivity that doesn't need a Blazor Server round trip. See [[coach-architecture]] for the backend layering this UI sits on.

## Build pipeline gotchas

- No `tailwind.config` — Tailwind v4's automatic content detection scans `Styles/` and `Components/` only (see the Dockerfile's `assets` stage and `scripts/build-web-assets.ps1`). A utility class only takes effect if it's literally written inside a file under `src/Coach.Web/Components/` (or `Styles/tailwind.css`) — don't build class names by string concatenation in C#, or the scanner won't see them.
- `wwwroot/css/` and `wwwroot/js/` are gitignored generated output. After adding/changing Tailwind classes or touching Alpine's vendored file, run `./scripts/build-web-assets.ps1` from the repo root before `dotnet run` locally, or the browser will serve stale/missing CSS.
- Alpine is vendored (no npm), core build only — no plugins (`x-collapse`, `x-mask`, etc. aren't available). `x-show`, `x-transition`, `x-data`, `x-bind`, `x-on`, `x-cloak` are all core and fine to use.

## The `@` collision (read this before writing Alpine in a .razor file)

Razor treats a bare `@` as the start of a C# expression. Alpine's shorthand for events is `@click="..."` — **never write that literally in a `.razor` file**; Razor will try to parse it as code. Always use the full `x-on:click="..."` form instead. Alpine's bind shorthand (`:class="..."`) is safe as-is since it starts with `:`, not `@`.

## App shell (MainLayout)

`MainLayout.razor` owns a fixed-height shell: `h-screen flex flex-col` on the root, a plain (non-sticky) `<header>` nav bar, then `<main class="flex min-h-0 flex-1 flex-col">@Body</main>`. Each page fills that remaining space and scrolls its own content (`min-h-0 flex-1 overflow-y-auto` on the page's scrollable region) rather than letting the whole document scroll — this keeps the header always visible without `position: sticky`.

**The `min-h-0` is load-bearing.** A flex child's default `min-height` is `auto`, which silently breaks `overflow-y-auto` containment inside a flex column (the child grows instead of scrolling). Any time you nest a scrollable region inside a flex column in this app, both the flex parent and the scrolling child need `min-h-0` (or `flex-1 min-h-0` together) — this is the most common way a new page's scroll area quietly stops scrolling.

Nav uses Blazor's built-in `<NavLink>` for active-route styling (works in static SSR, no JS needed) rather than Alpine. Since the inactive and active utility classes land in the same rendered `class` attribute, use `ActiveClass="bg-slate-900! text-white!"` (trailing `!` = Tailwind v4's important modifier) so the active state reliably wins regardless of generated CSS order.

## Coach-scoped pages and routing

Every coach-facing page is scoped by a `{CoachSlug}` route parameter — `/{coach}` (chat), `/{coach}/goals`, `/{coach}/reflections`. `/` is the `CoachPicker` landing page (one card per persona from `CoachPersonaRegistry.GetAll()`); there is no bare `/goals`.

- **Inherit `CoachScopedPage`** (`@inherits CoachScopedPage`, in `Components/`) rather than hand-rolling the parameter + validation. It exposes `[Parameter] CoachSlug`, the resolved `Persona`, and a `CancellationToken`; redirects to `not-found` on an unknown slug; and calls the `OnCoachChangedAsync()` override once per distinct coach.
- **Reset page state in `OnCoachChangedAsync()`, not `OnInitializedAsync`.** Navigating between coaches on the same route (`/career` → `/health`) *reuses the component instance* — `OnInitialized`/`OnParametersSet` semantics won't reload for you, and stale data/`CancellationTokenSource` from the previous coach will leak. The base cancels its `CancellationToken` on every switch (and on dispose), so in-flight work for the old coach can't land on the new one — pass `CancellationToken` (the base's) to every async call, don't create your own CTS.
- **`MainLayout` derives the active coach from the first URL path segment** (`/health/goals` → `health`), not a route parameter — it's not a routed component. It subscribes to `NavigationManager.LocationChanged`; the switcher pill and nav are hidden when the segment resolves to null (the picker, or an unknown slug).
- **Header nav links are built relative to the active slug**: `href="@_persona.Slug"`, `href="@($"{_persona.Slug}/goals")"`. Interpolating an `href` string in C# is fine — only *class names* must stay literal for the Tailwind scanner (see Build pipeline gotchas).
- **`CoachPersona.Name` vs `ShortName`**: `Name` is the full label ("Career Coach"); `ShortName` ("Career") is what goes in page `<h1>`s, `<PageTitle>`s, and the header switcher pill. `Tone` is a prompt-authoring attribute — never surface it as UI copy.

## Visual language

- **Palette**: slate is the neutral base (backgrounds `slate-50`, text `slate-900`/`slate-600`, borders `slate-200`/`slate-300`). One small indigo accent (`indigo-50`/`indigo-700`) for the coach switcher pill in the header. Red (`red-50`/`red-600`/`red-700`) is reserved for errors and overdue state — don't reach for it decoratively.
- **Shape**: `rounded-full` for pill controls (nav links, primary buttons, text inputs that aren't multi-line). `rounded-2xl` for cards and chat bubbles. `rounded-xl` for banners. `rounded-lg` for small nested controls (badges' container, per-item inputs).
- **Layout**: content lives in a centered `max-w-3xl mx-auto` column with `px-4 sm:px-6` edge padding — keep new pages consistent with this rather than introducing a new max-width.
- **Breakpoints**: mobile-first, only `sm:` (640px) is used so far since this is a narrow single-user tool (e.g. `grid-cols-1 sm:grid-cols-2`, `flex-col sm:flex-row`). Reach for `md:`/`lg:` only when a page's content genuinely benefits from more columns at wider widths, not by default.

## Interaction patterns (copy these rather than inventing new ones)

- **Dismissible banner** (errors): `x-data="{ show: true }"` on the wrapper, `x-show="show"` `x-transition`, a `✕` button with `x-on:click="show = false"`. Since Blazor re-adds/removes the whole `@if (_error is not null)` block as a fresh DOM node each time, `show` correctly resets to `true` on a new error without extra wiring.
- **Collapsible card**: `x-data="{ open: true }"` on the card, a header `<button x-on:click="open = !open">` containing a chevron `<svg>` with `x-bind:class="open ? 'rotate-180' : ''"`, and the body wrapped in `x-show="open" x-transition`. Always put `@key="<stable id>"` on the looped card element so Blazor preserves the DOM node (and thus the Alpine `open` state) across a server round trip instead of resetting it.
- **Progressive disclosure** (e.g. "+ Add action item"): a second flag in the same `x-data` (e.g. `adding: false`), a text trigger button, and the real form behind `x-show="adding" x-cloak x-transition`. Always add `x-cloak` to anything whose default state is hidden, to avoid a flash of visible content before Alpine initializes (`[x-cloak] { display: none !important; }` is already defined in `Styles/tailwind.css`).
- **Status/due badges**: small pill, `rounded-full px-1.5 py-0.5 text-[11px] font-medium`, neutral `bg-slate-100 text-slate-500` by default, `bg-red-50 text-red-600` for an alert state (e.g. overdue). Compute the alert condition in `@code`, not inline in the markup.

## When to reach for Alpine vs. Blazor state

Alpine only for interaction that's purely client-side and doesn't need server data (expand/collapse, show/hide a form section, dismiss a banner). Anything that reads or writes persisted state goes through a normal Blazor `@onclick`/`@onsubmit` handler and a server round trip — don't try to make Alpine talk to the server.
