using FlightTracker.Common;

namespace FlightTracker.Services.GoogleFlights;

/// <summary>
/// Builds Google Flights search link.
/// </summary>
public static class GoogleFlightsLinkBuilder
{
    public static string Build(string origin, string destination, string departureDate, string returnDate)
    {
        var completeLink = $"{origin} {destination} {departureDate} {returnDate}";
        return Constants.GoogleFlightsBase + Uri.EscapeDataString(completeLink);
    }
}
