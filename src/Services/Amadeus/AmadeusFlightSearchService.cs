using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using FlightTracker.Configuration;
using System.Globalization;
using FlightTracker.Common;

namespace FlightTracker.Services.Amadeus;

/// <summary>
/// Flight search implementation using Amadeus API (OAuth2 + Flight Offers Search).
/// </summary>
public class AmadeusFlightSearchService : IAmadeusFlightSearchService
{
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

    public async Task<decimal?> GetLowestPriceAsync(
        string origin, string destination, 
        string departureDate, string returnDate, 
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= Constants.MaxRetries; attempt++)
        {
            try
            {
                string? token = await GetAccessTokenAsync(cancellationToken);
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Failed to obtain Amadeus token");
                    return null;
                }

                HttpClient client = _httpClientFactory.CreateClient();
                string url = $"{Constants.AmadeusTestBase}/v2/shopping/flight-offers?" +
                    $"originLocationCode={origin}&" +
                    $"destinationLocationCode={destination}&" +
                    $"departureDate={departureDate}&" +
                    $"returnDate={returnDate}&" +
                    $"adults=1&" +
                    $"currencyCode=BRL&" +
                    $"travelClass=ECONOMY&" +
                    $"max=30";

                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    string body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Amadeus API returned {StatusCode}: {Body}", response.StatusCode, body);
                   
                    if (attempt < Constants.MaxRetries)
                        await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);

                    continue;
                }

                string json = await response.Content.ReadAsStringAsync(cancellationToken);
                return ParseLowestPrice(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attempt {Attempt}/{Max} failed to fetch Amadeus price", attempt, Constants.MaxRetries);
                if (attempt < Constants.MaxRetries)
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
            HttpClient client = _httpClientFactory.CreateClient();
            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _options.ClientId),
                new KeyValuePair<string, string>("client_secret", _options.ClientSecret)
            });

            using HttpResponseMessage response = await client.PostAsync($"{Constants.AmadeusTestBase}/v1/security/oauth2/token", form, cancellationToken);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Amadeus OAuth2 returned {StatusCode}. Response: {Body}. Check ClientId/ClientSecret in .env and use TEST environment credentials.", 
                    response.StatusCode, body);
                return null;
            }

            var doc = JsonDocument.Parse(body);
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
        var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) 
            return null;

        return data.EnumerateArray()
            .Select(offer => 
                offer.TryGetProperty("price", out var price) 
                    && price.TryGetProperty("grandTotal", out var total)
                    && decimal.TryParse(total.GetString(), NumberStyles.Any, 
                    CultureInfo.InvariantCulture, out var value)
                    ? (decimal?)value
                    : null)
            .Where(price => price.HasValue)
            .DefaultIfEmpty()
            .Min();
    }
}
