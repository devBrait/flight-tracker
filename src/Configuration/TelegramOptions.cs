namespace FlightTracker.Configuration;

/// <summary>
/// Telegram bot and chat options (bind from appsettings "Telegram").
/// </summary>
public class TelegramOptions
{
    public const string SectionName = "Telegram";

    /// <summary>Bot token from @BotFather.</summary>
    public string BotToken { get; set; } = string.Empty;

    /// <summary>Chat (or channel) ID that will receive notifications.</summary>
    public string ChatId { get; set; } = string.Empty;
}
