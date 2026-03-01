using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using FlightTracker.Configuration;

namespace FlightTracker.Services;

/// <summary>
/// Sends notifications via the official Telegram API (sendMessage).
/// Endpoint: https://api.telegram.org/bot{token}/sendMessage
/// </summary>
public class TelegramNotificationService : INotificationService
{
    private const string TelegramApiBase = "https://api.telegram.org";
    private const int MaxRetries = 3;
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

    /// <inheritdoc />
    public async Task SendFlightPriceAlertAsync(string origin, string destination, string departureDate, string? returnDate, decimal price, decimal? averageLast7Days, string googleFlightsLink, bool isTargetReached, CancellationToken cancellationToken = default)
    {
        var mediaLine = averageLast7Days.HasValue
            ? $"7-day average: R$ {averageLast7Days.Value:N2}"
            : "7-day average: N/A";
        var text = new StringBuilder();
        text.AppendLine(isTargetReached ? "🛫 *Target price reached!*" : "📋 *Verification summary*");
        text.AppendLine();
        text.AppendLine($"Origin: {EscapeMarkdown(origin)}");
        text.AppendLine($"Destination: {EscapeMarkdown(destination)}");
        text.AppendLine($"Outbound: {EscapeMarkdown(departureDate)}");
        if (!string.IsNullOrWhiteSpace(returnDate))
            text.AppendLine($"Return: {EscapeMarkdown(returnDate!)}");
        text.AppendLine($"Price found: R$ {price:N2}");
        text.AppendLine(mediaLine);
        if (!isTargetReached)
            text.AppendLine("_Above target price._");
        text.AppendLine();
        text.AppendLine($"[View on Google Flights]({googleFlightsLink})");

        var payload = new
        {
            chat_id = _options.ChatId,
            text = text.ToString(),
            parse_mode = "Markdown",
            disable_web_page_preview = true
        };
        var json = JsonSerializer.Serialize(payload);

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{TelegramApiBase}/bot{_options.BotToken}/sendMessage";
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Notification sent successfully to Telegram (chat {ChatId})", _options.ChatId);
                    return;
                }
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Telegram API returned {StatusCode}: {Body}", response.StatusCode, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attempt {Attempt}/{Max} failed to send Telegram notification", attempt, MaxRetries);
            }

            if (attempt < MaxRetries)
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
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
            .Replace("-", "\\-")
            .Replace("=", "\\=")
            .Replace("|", "\\|")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace(".", "\\.")
            .Replace("!", "\\!");
    }
}
