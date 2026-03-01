namespace FlightTracker.Services.Amadeus;

/// <summary>
/// Flight offer search service (lowest price for a route/date).
/// </summary>
public interface IAmadeusFlightSearchService
{
    /// <summary>
    /// Searches offers for the route and date(s) and returns the lowest price found.
    /// </summary>
    /// <param name="returnDate">Return date (YYYY-MM-DD); </param>
    /// <returns>Lowest price (one-way or round-trip).</returns>
    Task<decimal?> GetLowestPriceAsync(string origin, string destination, string departureDate, string? returnDate, CancellationToken cancellationToken = default);
}
