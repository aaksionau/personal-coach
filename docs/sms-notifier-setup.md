# SMS notifier + check-in texts setup

`Coach.Web`'s Check-in Scheduler sends the user two kinds of text, both through
the same SMS notifier:

- **Weekly digest** — once a week `WeeklyDigestScheduler` asks the model to write
  **one** consolidated check-in covering all four coaches, grounded in the current
  goals, action items, reflections, calendar, and values profile, not a template.
- **Due-date nudges** — daily `DueDateNudgeScheduler` texts a short model-written
  nudge for each open action item whose due date is within `Nudge:LeadTimeDays`
  (or recently overdue), grounded in that one action item. It keeps nudging (once
  per day, escalating) until the item is done.

Delivery is **one-way**: the text goes out through a Google Fi email-to-SMS
gateway and there is no inbound path. The user always replies by opening the web
app, never by texting back.

## How it works

- `Coach.Infrastructure.Sms` sends by SMTP through a Gmail account to
  `{digits-only ToNumber}@{GatewayDomain}` (Fi's email-to-SMS gateway) — the same
  transport shape as `weather-home-station`'s `SmsNotifier` (SmtpClient on 587 /
  STARTTLS, `MailMessage` with an empty subject, `msg.fi.google.com` gateway).
- `SmsMessageComposer` builds the `MailMessage` (recipient, from, plain-text body,
  no subject); `SmtpEmailSender` does the actual send over port 587 / STARTTLS.
- `SmtpSmsNotifier` **throws** when unconfigured or when the send fails — the
  scheduler logs it and waits for the next run rather than crashing the app, but
  the failure is visible in `kubectl logs` (unlike the calendar reader, which
  stays silent so a coach turn is never blocked). The nudge scheduler wraps each
  item, so one failed nudge doesn't block the others in that run (it retries next
  day, since only a sent nudge is written to the `DueDateNudge` log).
- `WeeklyDigestService` assembles the cross-coach context and calls the shared
  `AIAgent`. `DueDateNudgeService` does the same for a single action item via
  `DueDateNudgePromptComposer`. Like the Conversation Engine, their model-calling
  glue carries no tests by design; the composers and
  `CoachContextBuilder.BuildAllTrackedStatesAsync` are covered.

## Prerequisites

- A Gmail account to send from, with **2-Step Verification on** and an
  [**App Password**](https://myaccount.google.com/apppasswords) generated for it
  (Gmail rejects a plain account password over SMTP).
- The destination phone number on **Google Fi**. Fi's current email-to-SMS
  gateway domain is `msg.fi.google.com`; if Fi changes it, override
  `Sms:GatewayDomain` rather than editing code. Send yourself a test message to
  confirm the gateway before relying on it.

## Config

Bound from the `Sms` section (`src/Coach.Infrastructure.Sms/SmsOptions.cs`), the
`Digest` section (`src/Coach.Web/BackgroundServices/DigestOptions.cs`), and the
`Nudge` section (`src/Coach.Web/BackgroundServices/NudgeOptions.cs`):

| Setting                     | Value                                                   |
| --------------------------- | ------------------------------------------------------- |
| `Sms:SmtpUsername`          | the sending Gmail address (also the From address)       |
| `Sms:SmtpPassword`          | the Gmail **app password** (not the account password)   |
| `Sms:ToNumber`              | destination phone number (non-digits are stripped)      |
| `Sms:GatewayDomain`         | `msg.fi.google.com` (default)                           |
| `Sms:SmtpHost`              | `smtp.gmail.com` (default)                              |
| `Sms:SmtpPort`              | `587` (default)                                         |
| `Digest:Enabled`            | `true` (default); `false` keeps the scheduler dormant   |
| `Digest:DayOfWeek`          | `Monday` (default)                                      |
| `Digest:TimeOfDay`          | `08:00:00` (default), local to `Digest:TimeZoneId`      |
| `Digest:TimeZoneId`         | IANA id, `America/Chicago` (default); falls back to UTC |
| `Nudge:Enabled`             | `true` (default); `false` keeps the scheduler dormant   |
| `Nudge:TimeOfDay`           | `08:00:00` (default), local to `Nudge:TimeZoneId`       |
| `Nudge:TimeZoneId`          | IANA id, `America/Chicago` (default); falls back to UTC |
| `Nudge:LeadTimeDays`        | `4` (default) — nudge once a due date is this close     |
| `Nudge:StopAfterOverdueDays`| `7` (default) — stop nudging past this many days overdue|

**Local dev:** both schedulers are disabled in `appsettings.Development.json`.
To exercise a real send locally, set the `Sms:*` values via user-secrets and flip
`Digest:Enabled` / `Nudge:Enabled` back on (or invoke `WeeklyDigestService` /
`DueDateNudgeService` from a scratch harness):

```powershell
dotnet user-secrets --project src/Coach.Web set "Sms:SmtpUsername" "..."
dotnet user-secrets --project src/Coach.Web set "Sms:SmtpPassword" "..."
dotnet user-secrets --project src/Coach.Web set "Sms:ToNumber" "..."
```

**Production:** store `Sms:SmtpUsername` / `Sms:SmtpPassword` / `Sms:ToNumber` as a
Kubernetes secret in the `coach` namespace, projected as `Sms__SmtpUsername` /
`Sms__SmtpPassword` / `Sms__ToNumber` env vars — wired via the `04-coach-platform`
Terraform module in the sibling `home-server` repo, alongside the existing
`AzureAi__*` and `GoogleCalendar__*` secrets. Set `Digest__TimeZoneId` /
`Nudge__TimeZoneId` there if Central time isn't wanted.

## Notes

- Gmail SMTP caps a free account at ~500 recipients/day — a weekly digest plus a
  handful of daily nudges is far under any limit.
- If the app password is revoked (or 2SV is turned off), the send fails loudly on
  the next scheduled run; generate a new one and update the secret.
