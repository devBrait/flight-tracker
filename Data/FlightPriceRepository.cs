using Microsoft.EntityFrameworkCore;
using FlightTracker.Domain;

namespace FlightTracker.Data;

/// <summary>
/// Flight price repository implementation using EF Core.
/// </summary>
public class FlightPriceRepository : IFlightPriceRepository
{
    private readonly FlightTrackerDbContext _context;

    public FlightPriceRepository(FlightTrackerDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(FlightPrice flightPrice, CancellationToken cancellationToken = default)
    {
        await _context.FlightPrices.AddAsync(flightPrice, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<FlightPrice?> GetLastAsync(string origin, string destination, DateTime departureDate, DateTime? returnDate, CancellationToken cancellationToken = default)
    {
        var query = _context.FlightPrices
            .Where(p => p.Origin == origin && p.Destination == destination && p.DepartureDate.Date == departureDate.Date);
        if (returnDate.HasValue)
            query = query.Where(p => p.ReturnDate.HasValue && p.ReturnDate.Value.Date == returnDate.Value.Date);
        else
            query = query.Where(p => p.ReturnDate == null);
        return await query.OrderByDescending(p => p.CheckedAt).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<decimal?> GetLast7DaysAverageAsync(string origin, string destination, DateTime departureDate, DateTime? returnDate, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var query = _context.FlightPrices.Where(p =>
            p.Origin == origin &&
            p.Destination == destination &&
            p.DepartureDate.Date == departureDate.Date &&
            p.CheckedAt >= cutoff);
        if (returnDate.HasValue)
            query = query.Where(p => p.ReturnDate.HasValue && p.ReturnDate.Value.Date == returnDate.Value.Date);
        else
            query = query.Where(p => p.ReturnDate == null);
        return await query.AverageAsync(p => (decimal?)p.Price, cancellationToken);
    }
}
