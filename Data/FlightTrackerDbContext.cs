using Microsoft.EntityFrameworkCore;
using FlightTracker.Domain;

namespace FlightTracker.Data;

/// <summary>
/// SQLite DbContext for flight price persistence.
/// </summary>
public class FlightTrackerDbContext : DbContext
{
    public FlightTrackerDbContext(DbContextOptions<FlightTrackerDbContext> options)
        : base(options)
    {
    }

    public DbSet<FlightPrice> FlightPrices => Set<FlightPrice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FlightPrice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Origin).HasMaxLength(10);
            entity.Property(e => e.Destination).HasMaxLength(10);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.HasIndex(e => new { e.Origin, e.Destination, e.DepartureDate, e.ReturnDate, e.CheckedAt });
        });
    }
}
