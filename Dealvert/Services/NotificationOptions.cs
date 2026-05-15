namespace Alivert.Services;

public sealed class NotificationOptions
{
    public string? TelegramBotToken { get; set; }
    public int RequestTimeoutSeconds { get; set; } = 8;
    public EmailNotificationOptions Email { get; set; } = new();
}

public sealed class EmailNotificationOptions
{
    public string FromName { get; set; } = "Promovert";
    public string? FromEmail { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string SmtpServer { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public List<EmailSenderProfileOptions> SenderProfiles { get; set; } = new();
}

public sealed class EmailSenderProfileOptions
{
    public string Name { get; set; } = "Primary";
    public string FromName { get; set; } = "Promovert";
    public string? FromEmail { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? SmtpServer { get; set; }
    public int? SmtpPort { get; set; }
    public bool? EnableSsl { get; set; }
}
