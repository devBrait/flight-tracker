using FlightTracker.Domain;

namespace FlightTracker.Data;

/// <summary>
/// Repository for flight price persistence and queries.
/// </summary>
public interface IFlightPriceRepository
{
    /// <summary>Adds a new price record.</summary>
    Task AddAsync(FlightPrice flightPrice, CancellationToken cancellationToken = default);

    /// <summary>Returns the latest recorded price for the route and date(s).</summary>
    /// <param name="returnDate">Null = one-way; value = round-trip.</param>
    Task<FlightPrice?> GetLastAsync(string origin, string destination, DateTime departureDate, DateTime? returnDate, CancellationToken cancellationToken = default);

    /// <summary>Returns the average price over the last 7 days for the route and date(s).</summary>
    /// <param name="returnDate">Null = one-way; value = round-trip.</param>
    Task<decimal?> GetLast7DaysAverageAsync(string origin, string destination, DateTime departureDate, DateTime? returnDate, CancellationToken cancellationToken = default);
}
