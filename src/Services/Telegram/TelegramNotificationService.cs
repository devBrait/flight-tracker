using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using FlightTracker.Configuration;
using FlightTracker.Common;

namespace FlightTracker.Services.Telegram;

/// <summary>
/// Sends notifications via the official Telegram API (sendMessage).
/// </summary>
public class TelegramNotificationService : ITelegramNotificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TelegramNotificationService> _logger;
    private readonly TelegramOptions _options;

    public TelegramNotificationService(
        IHttpClientFactory httpClientFactory,
        IOptions<TelegramOptions> options,
        ILogger<TelegramNotificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    public async Task SendFlightPriceAlertAsync(string origin, string destination, string departureDate, 
        string? returnDate, decimal price, decimal? averageLast7Days, 
        string googleFlightsLink, bool isTargetReached, CancellationToken cancellationToken = default)
    {
        StringBuilder text = new StringBuilder();

        text.AppendLine();
        text.AppendLine("✈️ *Verification Summary!* \n");
        text.AppendLine($"Origin: {EscapeMarkdown(origin)}");
        text.AppendLine($"Destination: {EscapeMarkdown(destination)}");
        text.AppendLine($"Outbound: {EscapeMarkdown(departureDate)}");
        text.AppendLine($"Return: {EscapeMarkdown(returnDate!)}");
        text.AppendLine($"Price found: R$ {price:N2}");
        text.AppendLine();
        text.AppendLine($"[View on Google Flights]({googleFlightsLink})");

        var payload = new
        {
            chat_id = _options.ChatId,
            text = text.ToString(),
            parse_mode = "Markdown",
            disable_web_page_preview = true
        };
        string json = JsonSerializer.Serialize(payload);

        try
        {
            HttpClient client = _httpClientFactory.CreateClient("Telegram");
            string url = $"/bot{_options.BotToken}/sendMessage";

            using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Notification sent successfully to Telegram (chat {ChatId})", _options.ChatId);
                return;
            }

            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Telegram API returned {StatusCode}: {Body}", response.StatusCode, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Telegram notification");
        }
    }

    private static string EscapeMarkdown(string value)
    {
        return value
            .Replace("_", "\\_")
            .Replace("*", "\\*")
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("~", "\\~")
            .Replace("`", "\\`")
            .Replace(">", "\\>")
            .Replace("#", "\\#")
            .Replace("+", "\\+")
            .Replace("=", "\\=")
            .Replace("|", "\\|")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace(".", "\\.")
            .Replace("!", "\\!");
    }
}
