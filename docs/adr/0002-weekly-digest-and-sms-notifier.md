# 2. Weekly digest runs in the main app; SMS is its own adapter

Date: 2026-08-27

## Status

Accepted

## Context

The PRD calls for one consolidated text a week summarising due/notable items
across all four coaches, written by the model (not a template), delivered by SMTP
to a Google Fi email-to-SMS gateway — the pattern proven in
`weather-home-station`'s `SmsNotifier`. It is one-way: replies always happen in
the web app.

Two placement questions: where the scheduler lives, and whether the SMTP send
warrants its own project like `Coach.Infrastructure.Garmin` /
`Coach.Infrastructure.GoogleCalendar`.

## Decision

- **Scheduler in `Coach.Web`.** `WeeklyDigestScheduler` is a hosted
  `BackgroundService`, not a separate deployable. Unlike Garmin ingestion (its own
  CronJob because an unofficial login can break and must not touch chat — ADR 1),
  the digest only leans on already-deployed in-process pieces; a failed run is
  caught, logged, and retried next week. The `Digest` config section controls
  day/time/timezone and an on/off switch.

- **`WeeklyDigestService` is an Agent, separate from `CoachConversationEngine`.**
  It assembles cross-coach state via a new
  `CoachContextBuilder.BuildAllTrackedStatesAsync` plus calendar and values, then
  calls the shared `AIAgent`. Its model-calling glue is untested by design, like
  the Conversation Engine; the context assembly and the prompt composer are
  tested.

- **SMS gets its own adapter project (`Coach.Infrastructure.Sms`).** Even though
  `System.Net.Mail` is in the BCL (no heavy dependency, unlike `Google.Apis.*` or
  `Unofficial.Garmin.Connect`), a standalone project keeps every third-party
  integration on the same shape — flat layout, its own `AddCoachSms`, an
  `internal` seam (`ISmtpEmailSender`) wrapping the raw send so the pure
  `SmsMessageComposer` (recipient/message construction) is unit-tested with
  `[InternalsVisibleTo]`, no `public` surface beyond the port impl and the
  `Add...` method.

- **The notifier throws; the scheduler swallows.** `SmtpSmsNotifier` surfaces a
  loud failure (unconfigured, or a failed send) — a weekly text that silently
  never arrives is worse than a logged error. The scheduler catches around the
  whole run so one bad week never crashes the web app.

- **One-way by construction.** `ISmsNotifier` has only `SendAsync`. No inbound
  email/SMS ingestion exists anywhere in the codebase.

## Consequences

- The digest shares the web app's lifecycle: a deploy or restart re-arms the
  schedule (next occurrence is recomputed on each loop, so a restart never
  double-sends or skips).
- The Fi gateway domain can change; it is config (`Sms:GatewayDomain`), not code.
- The k8s `Sms__*` secret and any non-default `Digest__*` values live in the
  `home-server` repo, consistent with `AzureAi__*` and `GoogleCalendar__*`.
