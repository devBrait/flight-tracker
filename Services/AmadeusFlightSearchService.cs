using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using FlightTracker.Configuration;

namespace FlightTracker.Services;

/// <summary>
/// Flight search implementation using Amadeus API (OAuth2 + Flight Offers Search).
/// Uses HttpClientFactory and simple retry on failure.
/// </summary>
public class AmadeusFlightSearchService : IFlightSearchService
{
    private const string AmadeusTestBase = "https://test.api.amadeus.com";
    private const int MaxRetries = 3;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AmadeusFlightSearchService> _logger;
    private readonly AmadeusOptions _options;

    public AmadeusFlightSearchService(
        IHttpClientFactory httpClientFactory,
        IOptions<AmadeusOptions> options,
        ILogger<AmadeusFlightSearchService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<decimal?> GetLowestPriceAsync(string origin, string destination, string departureDate, string? returnDate, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var token = await GetAccessTokenAsync(cancellationToken);
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Failed to obtain Amadeus token");
                    return null;
                }

                var client = _httpClientFactory.CreateClient();

                var url = $"{AmadeusTestBase}/v2/shopping/flight-offers?" +
                        $"originLocationCode={origin}&" +
                        $"destinationLocationCode={destination}&" +
                        $"departureDate={departureDate}&" +
                        $"adults=1&" +
                        $"currencyCode=BRL";

                if (!string.IsNullOrWhiteSpace(returnDate))
                    url += $"&returnDate={returnDate!.Trim()}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Amadeus API returned {StatusCode}: {Body}", response.StatusCode, body);
                    if (attempt < MaxRetries)
                        await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
                    continue;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                return ParseLowestPrice(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attempt {Attempt}/{Max} failed to fetch Amadeus price", attempt, MaxRetries);
                if (attempt < MaxRetries)
                    await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
        }

        return null;
    }

    /// <summary>
    /// Gets OAuth2 token (Client Credentials) from Amadeus.
    /// </summary>
    private async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _options.ClientId),
                new KeyValuePair<string, string>("client_secret", _options.ClientSecret)
            });
            using var response = await client.PostAsync($"{AmadeusTestBase}/v1/security/oauth2/token", form, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Amadeus OAuth2 returned {StatusCode}. Response: {Body}. Check ClientId/ClientSecret in .env and use TEST environment credentials.", response.StatusCode, body);
                return null;
            }
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("access_token", out var tokenProp)
                ? tokenProp.GetString()
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obtaining Amadeus OAuth2 token");
            return null;
        }
    }

    /// <summary>
    /// Parses the lowest price from the Flight Offers API response JSON.
    /// Format: { "data": [ { "price": { "grandTotal": "2800.50" } }, ... ] }
    /// </summary>
    private static decimal? ParseLowestPrice(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                return null;
            decimal? min = null;
            foreach (var offer in data.EnumerateArray())
            {
                if (!offer.TryGetProperty("price", out var price) || !price.TryGetProperty("grandTotal", out var total))
                    continue;
                var totalStr = total.GetString();
                if (string.IsNullOrEmpty(totalStr) || !decimal.TryParse(totalStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var value))
                    continue;
                if (min == null || value < min.Value)
                    min = value;
            }
            return min;
        }
        catch
        {
            return null;
        }
    }
}
