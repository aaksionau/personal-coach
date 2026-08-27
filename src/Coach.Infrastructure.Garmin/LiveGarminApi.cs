using global::Garmin.Connect;
using global::Garmin.Connect.Auth;
using Microsoft.Extensions.Options;

namespace Coach.Infrastructure.Garmin;

/// <summary>
/// The live <see cref="IGarminApi"/>: logs into Garmin Connect with a username/password (no MFA)
/// and runs the three read calls that back a daily metric record. The <see cref="GarminConnectClient"/>
/// and its <see cref="HttpClient"/> are built lazily on first use so an unconfigured app still
/// starts; a session token is cached in-memory for the process lifetime by the SDK.
/// </summary>
internal sealed class LiveGarminApi : IGarminApi, IDisposable
{
    private readonly GarminOptions _options;
    private readonly Lazy<(HttpClient Http, GarminConnectClient Client)> _client;

    public LiveGarminApi(IOptions<GarminOptions> options)
    {
        _options = options.Value;
        _client = new Lazy<(HttpClient, GarminConnectClient)>(CreateClient);
    }

    public async Task<GarminDaySnapshot> GetDayAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var day = date.ToDateTime(TimeOnly.MinValue);
        var client = _client.Value.Client;

        var summary = await client.GetUserSummary(day, cancellationToken);
        var sleep = await client.GetWellnessSleepData(day, cancellationToken);
        var activities = await client.GetActivitiesByDate(day, day, activityType: "", cancellationToken);

        return new GarminDaySnapshot(summary, sleep, activities ?? []);
    }

    private (HttpClient, GarminConnectClient) CreateClient()
    {
        var http = new HttpClient();
        var auth = new BasicAuthParameters(_options.Email, _options.Password);
        return (http, new GarminConnectClient(new GarminConnectContext(http, auth)));
    }

    public void Dispose()
    {
        if (_client.IsValueCreated)
        {
            _client.Value.Http.Dispose();
        }
    }
}
