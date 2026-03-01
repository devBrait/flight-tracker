using FlightTracker.Configuration;
using FlightTracker.Services.Amadeus;
using FlightTracker.Services.GoogleFlights;
using FlightTracker.Services.Telegram;
using Microsoft.Extensions.Options;

namespace FlightTracker.Workers;

/// <summary>
/// Runs once on startup: fetches lowest price from Amadeus, compares to target,
/// sends Telegram price (stateless, cron-friendly).
/// </summary>
public class PriceMonitorWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<PriceMonitorWorker> _logger;
    private readonly FlightOptions _flightOptions;

    public PriceMonitorWorker(
        IServiceProvider serviceProvider,
        IHostApplicationLifetime lifetime,
        IOptions<FlightOptions> flightOptions,
        ILogger<PriceMonitorWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _lifetime = lifetime;
        _logger = logger;
        _flightOptions = flightOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RunOnceAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Price check failed");
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        var flightSearch = scope.ServiceProvider.GetRequiredService<IAmadeusFlightSearchService>();
        var notification = scope.ServiceProvider.GetRequiredService<ITelegramNotificationService>();

        var origin = _flightOptions.Origin;
        var destination = _flightOptions.Destination;
        var departureDateStr = _flightOptions.DepartureDate;
        var returnDateStr = _flightOptions.ReturnDate;

        if (string.IsNullOrWhiteSpace(origin) 
            || string.IsNullOrWhiteSpace(destination) 
            || string.IsNullOrWhiteSpace(departureDateStr)
            || string.IsNullOrEmpty(returnDateStr))
        {
            _logger.LogWarning("Incomplete flight configuration (Origin/Destination/DepartureDate). Check appsettings or env.");
            return;
        }

        // 1. Fetch lowest price from Amadeus
        decimal? price = await flightSearch.GetLowestPriceAsync(origin, destination, departureDateStr, returnDateStr, cancellationToken);
        if (!price.HasValue)
        {
            _logger.LogWarning("No price found for {Origin}-{Destination} {Departure}{Return}",
                origin, destination, departureDateStr, returnDateStr != null ? $" (return {returnDateStr})" : "");
            return;
        }

        _logger.LogInformation("Price found: R$ {Price:N2} ({Origin} -> {Destination})", price.Value, origin, destination);

        // 2. Send Telegram (price <= target)
        var link = GoogleFlightsLinkBuilder.Build(origin, destination, departureDateStr, returnDateStr);
        await notification.SendFlightPriceAlertAsync(origin, destination, departureDateStr, returnDateStr, 
            price.Value, null, link, isTargetReached: true, cancellationToken);
        
        _logger.LogInformation("Telegram notification sent (price R$ {Price:N2} found).", price.Value);
    }
}
