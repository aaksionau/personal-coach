using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Options;

namespace Coach.Infrastructure.GoogleCalendar;

/// <summary>
/// The live <see cref="IGoogleCalendarEvents"/>: authenticates with a stored refresh token against
/// the <b>read-only</b> calendar scope (<see cref="CalendarService.Scope.CalendarReadonly"/> -- the
/// library cannot mint a token that can write) and runs a single <c>events.list</c> for the given
/// window. The underlying <see cref="CalendarService"/> is built lazily on first use so an
/// unconfigured app still starts.
/// </summary>
internal sealed class GoogleCalendarEvents : IGoogleCalendarEvents, IDisposable
{
    private const int MaxEvents = 100;

    private readonly GoogleCalendarOptions _options;
    private readonly Lazy<CalendarService> _service;

    public GoogleCalendarEvents(IOptions<GoogleCalendarOptions> options)
    {
        _options = options.Value;
        _service = new Lazy<CalendarService>(() => CreateService(_options));
    }

    public async Task<Events> ListAsync(DateTimeOffset timeMin, DateTimeOffset timeMax, CancellationToken cancellationToken)
    {
        var request = _service.Value.Events.List(_options.CalendarId);
        request.TimeMinDateTimeOffset = timeMin;
        request.TimeMaxDateTimeOffset = timeMax;
        request.SingleEvents = true;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
        request.ShowDeleted = false;
        request.MaxResults = MaxEvents;
        return await request.ExecuteAsync(cancellationToken);
    }

    private static CalendarService CreateService(GoogleCalendarOptions options)
    {
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = options.ClientId, ClientSecret = options.ClientSecret },
            Scopes = [CalendarService.Scope.CalendarReadonly],
        });

        var credential = new UserCredential(flow, "user", new TokenResponse { RefreshToken = options.RefreshToken });

        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "PersonalCoach",
        });
    }

    public void Dispose()
    {
        if (_service.IsValueCreated)
        {
            _service.Value.Dispose();
        }
    }
}
