namespace FlightTracker.Services;

/// <summary>
/// Flight offer search service (lowest price for a route/date).
/// </summary>
public interface IFlightSearchService
{
    /// <summary>
    /// Searches offers for the route and date(s) and returns the lowest price found.
    /// </summary>
    /// <param name="returnDate">Return date (YYYY-MM-DD); null or empty = one-way only.</param>
    /// <returns>Lowest price (one-way or round-trip), or null if no offers/error.</returns>
    Task<decimal?> GetLowestPriceAsync(string origin, string destination, string departureDate, string? returnDate, CancellationToken cancellationToken = default);
}
