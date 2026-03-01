namespace FlightTracker.Configuration;

/// <summary>
/// Options for the flight route to monitor (bind from appsettings "Flight").
/// </summary>
public class FlightOptions
{
    public const string SectionName = "Flight";

    /// <summary>Origin airport IATA code (e.g. GRU).</summary>
    public string Origin { get; set; } = string.Empty;

    /// <summary>Destination airport IATA code (e.g. MCO).</summary>
    public string Destination { get; set; } = string.Empty;

    /// <summary>Outbound date in YYYY-MM-DD format.</summary>
    public string DepartureDate { get; set; } = string.Empty;

    /// <summary>Return date in YYYY-MM-DD format. Empty = one-way only.</summary>
    public string ReturnDate { get; set; } = string.Empty;

    /// <summary>Target price; when price is &lt;= this value, the message shows "target reached".</summary>
    public decimal TargetPrice { get; set; }
}
