namespace FlightTracker.Services;

/// <summary>
/// Builds Google Flights search link (one-way or round-trip).
/// </summary>
public static class GoogleFlightsLinkBuilder
{
    /// <summary>
    /// One-way: q=ORIGIN DESTINATION YYYY-MM-DD. Round-trip: includes return date in query.
    /// </summary>
    public static string Build(string origin, string destination, string departureDate, string? returnDate = null)
    {
        var q = string.IsNullOrWhiteSpace(returnDate)
            ? $"{origin} {destination} {departureDate}"
            : $"{origin} {destination} {departureDate} {returnDate.Trim()}";
        return "https://www.google.com/travel/flights?q=" + Uri.EscapeDataString(q);
    }
}
