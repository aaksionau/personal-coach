# Google Calendar setup (one-time, HITL)

The Calendar Reader (`Coach.Infrastructure.GoogleCalendar`) reads the user's
upcoming events **read-only** and folds them into every coach's context. It needs
a Google Cloud OAuth client plus a one-time browser authorization that only the
app's user can complete. Until that's done the reader simply returns no events
and the app runs normally.

## 1. Create the OAuth client (Google Cloud Console)

1. Create (or reuse) a Google Cloud project.
2. **APIs & Services → Library →** enable **Google Calendar API**.
3. **APIs & Services → OAuth consent screen:**
   - User type **External**, publishing status **Testing**.
   - Add the single Google account that owns the calendar as a **Test user**.
   - Add the scope `https://www.googleapis.com/auth/calendar.readonly` (read-only).
4. **APIs & Services → Credentials → Create credentials → OAuth client ID:**
   - Application type **Desktop app**.
   - Save the **Client ID** and **Client secret**.

## 2. Mint a refresh token (one-time, browser)

Easiest path — [OAuth 2.0 Playground](https://developers.google.com/oauthplayground/):

1. Gear icon → **Use your own OAuth credentials** → paste the client ID/secret.
2. In the scope list on the left, select
   **Calendar API v3 → `https://www.googleapis.com/auth/calendar.readonly`**.
3. **Authorize APIs**, sign in as the test user, consent.
4. **Exchange authorization code for tokens** → copy the **Refresh token**.

(Alternatively, run a throwaway console app calling
`GoogleWebAuthorizationBroker.AuthorizeAsync` with the same client secrets and the
`CalendarService.Scope.CalendarReadonly` scope, then read the refresh token out of
the generated token store.)

## 3. Provide the config

Bound from the `GoogleCalendar` section
(`src/Coach.Infrastructure.GoogleCalendar/GoogleCalendarOptions.cs`):

| Setting                    | Value                          |
| -------------------------- | ------------------------------ |
| `GoogleCalendar:ClientId`     | OAuth client ID             |
| `GoogleCalendar:ClientSecret` | OAuth client secret         |
| `GoogleCalendar:RefreshToken` | refresh token from step 2   |
| `GoogleCalendar:CalendarId`   | `primary` (default) or a specific calendar id |

**Local dev:**

```powershell
dotnet user-secrets --project src/Coach.Web set "GoogleCalendar:ClientId" "..."
dotnet user-secrets --project src/Coach.Web set "GoogleCalendar:ClientSecret" "..."
dotnet user-secrets --project src/Coach.Web set "GoogleCalendar:RefreshToken" "..."
```

**Production:** store the three values as a Kubernetes secret in the `coach`
namespace and project them as `GoogleCalendar__ClientId` /
`GoogleCalendar__ClientSecret` / `GoogleCalendar__RefreshToken` env vars — wired
via the `04-coach-platform` Terraform module in the sibling `home-server` repo,
alongside the existing `AzureAi__*` secret.

## Notes

- The refresh token is long-lived but Google may expire it if unused for 6 months
  or if the account's password changes — re-run step 2 to replace it.
- Read-only is enforced by the requested scope: the OAuth grant cannot produce a
  token that writes to the calendar, regardless of app code.
