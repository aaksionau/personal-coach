# 3. Ad-hoc due-date nudges are a sibling scheduler over a nudge-log table

Date: 2026-08-27

## Status

Accepted

## Context

The PRD's Check-in Scheduler "drives two triggers": the weekly consolidated
digest (ADR 0002, shipped) and ad-hoc nudges as individual action-item due dates
approach. Both go through the same one-way SMS Notifier (`ISmsNotifier`). Issue
#12 adds the second trigger.

Open questions: where the second trigger lives relative to `WeeklyDigestScheduler`;
how a nudge is generated and kept grounded in one action item; how a repeated
nudge for the same deadline is deduped; and how a nudge avoids echoing the weekly
digest when both fire in the same week.

## Decision

- **A separate `DueDateNudgeScheduler` hosted `BackgroundService`, not a second
  trigger bolted onto `WeeklyDigestScheduler`.** The weekly loop sleeps until a
  day-of-week/time; the nudge loop wakes daily. Keeping them apart lets each stay
  a thin delay loop over `CheckInSchedule` (renamed from `DigestSchedule`, now
  with `NextDailyOccurrenceUtc` alongside `NextOccurrenceUtc`). "One scheduler,
  two triggers" is the conceptual framing; two sibling services is the
  implementation.

- **A `DueDateNudge` log table, one row per action item per local calendar day a
  nudge was sent.** `GetPendingNudgesAsync` excludes items already nudged
  *today*, so a restart or a double run never double-texts. Because the row is
  written only after a successful send, a failed item resurfaces in the next
  day's run.

- **Escalating, not one-and-done.** An open action item keeps nudging daily while
  its due date is within `LeadTimeDays` (default 4) or up to
  `StopAfterOverdueDays` (default 7) past due, until it is marked Done. The
  prompt is told the signed days-to-due and the prior-nudge count, so the model's
  urgency rises on its own rather than via separate templates.

- **Per-item generate + send + record, each wrapped.** `DueDateNudgeService`
  resolves the persona for the item's coach, loads the values profile, and calls
  `DueDateNudgePromptComposer` (pure, tested). The scheduler catches around each
  item so one model or SMS failure doesn't block the rest of the run, and around
  the whole run so one bad day never crashes the web app — mirroring
  `WeeklyDigestScheduler`.

- **Digest overlap is handled by prompt instruction only.** The nudge prompt
  states it is a focused single-item reminder, "not the weekly cross-coach
  check-in", and must not summarise other goals. Suppressing recently-nudged
  items from the weekly digest itself was considered and deliberately deferred —
  it means threading nudge state into `WeeklyDigestService` for a soft,
  low-stakes dedup.

- **No new sending path.** The nudge reuses `ISmsNotifier.SendAsync` and the
  shared `"Sms"` config; only a `"Nudge"` section (enable/time/zone/window) is
  added.

- **Untested by design, per repo convention.** `DueDateNudgeService`'s model call
  and the scheduler's delay loop carry no tests (like `WeeklyDigestService` /
  `WeeklyDigestScheduler`); `DueDateNudgeStore` is an EF adapter, untested like
  `GoalStore`. Coverage lands on `DueDateNudgePromptComposer` and
  `CheckInSchedule.NextDailyOccurrenceUtc`.

## Consequences

- The nudge scheduler shares the web app's lifecycle; the next daily occurrence
  is recomputed each loop, so a restart never double-sends or skips.
- Moving an action item's due date out re-arms nudging for the new date (the
  dedup key is action item + calendar day, not the deadline).
- `Nudge__*` non-default values live in the `home-server` repo alongside
  `Digest__*` and `Sms__*`. Local/dev keeps `Nudge:Enabled=false`.
