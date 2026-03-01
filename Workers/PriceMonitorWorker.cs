using FlightTracker.Configuration;
using FlightTracker.Data;
using FlightTracker.Domain;
using FlightTracker.Services;
using Microsoft.Extensions.Options;

namespace FlightTracker.Workers;

/// <summary>
/// Worker that runs on startup and every 24h: fetches price, saves to DB, computes 7-day average,
/// and always sends a Telegram notification (indicating whether the target was reached).
/// </summary>
public class PriceMonitorWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PriceMonitorWorker> _logger;
    private readonly FlightOptions _flightOptions;

    public PriceMonitorWorker(
        IServiceProvider serviceProvider,
        IOptions<FlightOptions> flightOptions,
        ILogger<PriceMonitorWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _flightOptions = flightOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // First run on startup
        await RunOnceAsync(stoppingToken);

        // Then every 24 hours
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await DoWorkAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Do not let the worker stop on failure; only log
            _logger.LogError(ex, "Error in price monitor run; next run in 24h");
        }
    }

    private async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var flightSearch = scope.ServiceProvider.GetRequiredService<IFlightSearchService>();
        
        var repository = scope.ServiceProvider.GetRequiredService<IFlightPriceRepository>();
        var notification = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var origin = _flightOptions.Origin;
        var destination = _flightOptions.Destination;
        
        var departureDateStr = _flightOptions.DepartureDate;
        var returnDateStr = string.IsNullOrWhiteSpace(_flightOptions.ReturnDate) ? null : _flightOptions.ReturnDate!.Trim();
        
        var targetPrice = _flightOptions.TargetPrice;
        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination) || string.IsNullOrWhiteSpace(departureDateStr))
        {
            _logger.LogWarning("Incomplete flight configuration (Origin/Destination/DepartureDate). Check appsettings.");
            return;
        }

        // Fetch lowest price from API (one-way or round-trip)
        var price = await flightSearch.GetLowestPriceAsync(origin, destination, departureDateStr, returnDateStr, cancellationToken);
        if (!price.HasValue)
        {
            _logger.LogWarning("No price found for {Origin}-{Destination} {Departure}{Return}",
                origin, destination, departureDateStr, returnDateStr != null ? $" (return {returnDateStr})" : "");
            return;
        }

        _logger.LogInformation("Price found: R$ {Price:N2} ({Origin} -> {Destination}, outbound {Date}{Return})",
            price.Value, origin, destination, departureDateStr, returnDateStr != null ? $", return {returnDateStr}" : "");

        // Save to database
        var departureDate = DateTime.Parse(departureDateStr);
        var returnDate = !string.IsNullOrWhiteSpace(returnDateStr) ? (DateTime?)DateTime.Parse(returnDateStr) : null;
        
        var entity = new FlightPrice
        {
            Origin = origin,
            Destination = destination,
            DepartureDate = departureDate,
            ReturnDate = returnDate,
            Price = price.Value,
            CheckedAt = DateTime.UtcNow
        };
        await repository.AddAsync(entity, cancellationToken);

        // 7-day average (same route and dates)
        var averageLast7 = await repository.GetLast7DaysAverageAsync(origin, destination, departureDate, returnDate, cancellationToken);
       
        if (averageLast7.HasValue)
            _logger.LogInformation("7-day average: R$ {Average:N2}", averageLast7.Value);
        else
            _logger.LogInformation("7-day average: N/A (insufficient data)");

        // Always send Telegram notification with the price found (and whether target was reached)
        var link = GoogleFlightsLinkBuilder.Build(origin, destination, departureDateStr, returnDateStr);
        var isTargetReached = price.Value <= targetPrice;
        
        await notification.SendFlightPriceAlertAsync(origin, destination, departureDateStr, returnDateStr, price.Value, averageLast7, link, isTargetReached, cancellationToken);
        _logger.LogInformation("Telegram notification sent. Price R$ {Price:N2} ({Status})", price.Value, isTargetReached ? "target reached" : "above target");
    }
}
