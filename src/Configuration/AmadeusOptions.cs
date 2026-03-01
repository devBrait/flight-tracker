namespace FlightTracker.Configuration;

/// <summary>
/// Amadeus API authentication options (bind from appsettings "Amadeus").
/// </summary>
public class AmadeusOptions
{
    public const string SectionName = "Amadeus";

    /// <summary>App Client ID from Amadeus for Developers portal.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>App Client Secret.</summary>
    public string ClientSecret { get; set; } = string.Empty;
}
