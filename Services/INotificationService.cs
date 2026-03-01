namespace FlightTracker.Services;

/// <summary>
/// Notification delivery service (e.g. target price reached).
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a flight price notification (origin, destination, dates, price, 7-day average, link).
    /// </summary>
    /// <param name="returnDate">Return date; null = one-way only.</param>
    /// <param name="isTargetReached">True = title "Target price reached!"; false = "Verification summary".</param>
    Task SendFlightPriceAlertAsync(string origin, string destination, string departureDate, string? returnDate, decimal price, decimal? averageLast7Days, string googleFlightsLink, bool isTargetReached, CancellationToken cancellationToken = default);
}
