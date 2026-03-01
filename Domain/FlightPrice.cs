namespace FlightTracker.Domain;

/// <summary>
/// Entity representing a recorded flight price check. Used for history and average calculation.
/// </summary>
public class FlightPrice
{
    public int Id { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureDate { get; set; }
    /// <summary>Return date; null = one-way only.</summary>
    public DateTime? ReturnDate { get; set; }
    public decimal Price { get; set; }
    public DateTime CheckedAt { get; set; }
}
