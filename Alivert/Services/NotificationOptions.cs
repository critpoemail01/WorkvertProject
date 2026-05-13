namespace Alivert.Services;

public sealed class NotificationOptions
{
    public string? TelegramBotToken { get; set; }
    public int RequestTimeoutSeconds { get; set; } = 8;
}
