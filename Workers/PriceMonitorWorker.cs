using FlightTracker.Configuration;
using FlightTracker.Services;
using Microsoft.Extensions.Options;

namespace FlightTracker.Workers;

/// <summary>
/// Runs once on startup: fetches lowest price from Amadeus, compares to target,
/// sends Telegram only if price &lt;= target, then exits (stateless, cron-friendly).
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
        catch (OperationCanceledException)
        {
            // Expected when host is stopping
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
        using var scope = _serviceProvider.CreateScope();
        var flightSearch = scope.ServiceProvider.GetRequiredService<IFlightSearchService>();
        var notification = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var origin = _flightOptions.Origin;
        var destination = _flightOptions.Destination;
        var departureDateStr = _flightOptions.DepartureDate;
        var returnDateStr = string.IsNullOrWhiteSpace(_flightOptions.ReturnDate) ? null : _flightOptions.ReturnDate!.Trim();
        var targetPrice = _flightOptions.TargetPrice;

        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination) || string.IsNullOrWhiteSpace(departureDateStr))
        {
            _logger.LogWarning("Incomplete flight configuration (Origin/Destination/DepartureDate). Check appsettings or env.");
            return;
        }

        // 1. Fetch lowest price from Amadeus
        var price = await flightSearch.GetLowestPriceAsync(origin, destination, departureDateStr, returnDateStr, cancellationToken);
        if (!price.HasValue)
        {
            _logger.LogWarning("No price found for {Origin}-{Destination} {Departure}{Return}",
                origin, destination, departureDateStr, returnDateStr != null ? $" (return {returnDateStr})" : "");
            return;
        }

        _logger.LogInformation("Price found: R$ {Price:N2} ({Origin} -> {Destination})", price.Value, origin, destination);

        // 2–3. Compare to target
        if (price.Value > targetPrice)
        {
            _logger.LogInformation("Price R$ {Price:N2} is above target R$ {Target:N2}. No notification sent.", price.Value, targetPrice);
            return;
        }

        // 4. Send Telegram (price <= target)
        var link = GoogleFlightsLinkBuilder.Build(origin, destination, departureDateStr, returnDateStr);
        await notification.SendFlightPriceAlertAsync(origin, destination, departureDateStr, returnDateStr, price.Value, null, link, isTargetReached: true, cancellationToken);
        _logger.LogInformation("Telegram notification sent (price R$ {Price:N2} <= target R$ {Target:N2}).", price.Value, targetPrice);
    }
}
