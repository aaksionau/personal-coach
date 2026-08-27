namespace Coach.Infrastructure.Garmin;

/// <summary>
/// Config for the Garmin Connect adapter, bound from the <c>"Garmin"</c> section. The
/// email/password are for a personal Garmin account with <b>no MFA</b> (see the PRD) -- that's
/// what makes an unattended daily login viable. Blank by default so the app/job still boots
/// without them; the ingestion job then fails loudly when it actually tries to run.
/// </summary>
internal sealed class GarminOptions
{
    public const string SectionName = "Garmin";

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
}
