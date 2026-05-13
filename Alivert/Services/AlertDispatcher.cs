using Alivert.Data;
using Alivert.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Json;

namespace Alivert.Services;

public sealed class AlertDispatcher : IAlertDispatcher
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<NotificationOptions> _options;
    private readonly ILogger<AlertDispatcher> _logger;

    public AlertDispatcher(
        ApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<NotificationOptions> options,
        ILogger<AlertDispatcher> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task DispatchAsync(Alert alert, MarketSnapshot snapshot, string message, CancellationToken ct)
    {
        var trigger = new AlertTrigger
        {
            AlertId = alert.Id,
            TriggeredAtUtc = DateTime.UtcNow,
            Message = message,
            SnapshotJson = JsonSerializer.Serialize(snapshot)
        };

        _db.AlertTriggers.Add(trigger);

        var settings = await _db.UserNotificationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == alert.UserId, ct);

        var delivery = await DeliverAsync(alert, snapshot, message, settings, ct);
        delivery.AlertTrigger = trigger;
        _db.AlertDeliveryLogs.Add(delivery);
    }

    private async Task<AlertDeliveryLog> DeliverAsync(
        Alert alert,
        MarketSnapshot snapshot,
        string message,
        UserNotificationSettings? settings,
        CancellationToken ct)
    {
        var channel = (alert.Channel ?? "Email").Trim();
        var log = new AlertDeliveryLog
        {
            AlertId = alert.Id,
            UserId = alert.UserId,
            Symbol = alert.Symbol,
            Channel = channel,
            CreatedAtUtc = DateTime.UtcNow
        };

        try
        {
            var scheduleSkipReason = GetScheduleSkipReason(settings, DateTime.UtcNow);
            if (scheduleSkipReason is not null)
                return Skip(log, scheduleSkipReason);

            return channel.ToUpperInvariant() switch
            {
                "WEBHOOK" => await DeliverWebhookAsync(log, settings?.WebhookUrl, alert, snapshot, message, ct),
                "DISCORD" => await DeliverDiscordAsync(log, settings?.DiscordWebhookUrl, message, ct),
                "SLACK" => await DeliverSlackAsync(log, settings?.SlackWebhookUrl, message, ct),
                "TEAMS" => await DeliverTeamsAsync(log, settings?.TeamsWebhookUrl, message, ct),
                "TELEGRAM" => await DeliverTelegramAsync(log, settings?.TelegramChatId, message, ct),
                "EMAIL" => await DeliverEmailAsync(log, alert, snapshot, message, settings, ct),
                _ => Skip(log, $"Unsupported channel '{channel}'.")
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Notification delivery failed for alert {AlertId}.", alert.Id);
            log.Status = "Failed";
            log.ErrorMessage = Truncate(ex.Message, 500);
            return log;
        }
    }

    private async Task<AlertDeliveryLog> DeliverWebhookAsync(
        AlertDeliveryLog log,
        string? url,
        Alert alert,
        MarketSnapshot snapshot,
        string message,
        CancellationToken ct)
    {
        if (!TryAbsoluteHttpUri(url, out var uri))
            return Skip(log, "Webhook URL is not configured.");

        var payload = new
        {
            type = "alivert.alert_triggered",
            alertId = alert.Id,
            alert.Symbol,
            ruleType = alert.RuleType.ToString(),
            alert.Threshold,
            snapshot.Price,
            snapshot.PercentChange24h,
            snapshot.Volume24h,
            snapshot.AsOfUtc,
            message
        };

        return await PostJsonAsync(log, uri, payload, "Configured webhook", ct);
    }

    private async Task<AlertDeliveryLog> DeliverDiscordAsync(
        AlertDeliveryLog log,
        string? url,
        string message,
        CancellationToken ct)
    {
        if (!TryAbsoluteHttpUri(url, out var uri))
            return Skip(log, "Discord webhook URL is not configured.");

        var payload = new
        {
            content = Truncate($"**Alivert alert**\n{message}", 1900),
            allowed_mentions = new { parse = Array.Empty<string>() }
        };

        return await PostJsonAsync(log, uri, payload, "Discord webhook", ct);
    }

    private async Task<AlertDeliveryLog> DeliverSlackAsync(
        AlertDeliveryLog log,
        string? url,
        string message,
        CancellationToken ct)
    {
        if (!TryAbsoluteHttpUri(url, out var uri))
            return Skip(log, "Slack webhook URL is not configured.");

        var payload = new
        {
            text = Truncate($"Alivert alert\n{message}", 2900)
        };

        return await PostJsonAsync(log, uri, payload, "Slack webhook", ct);
    }

    private async Task<AlertDeliveryLog> DeliverTeamsAsync(
        AlertDeliveryLog log,
        string? url,
        string message,
        CancellationToken ct)
    {
        if (!TryAbsoluteHttpUri(url, out var uri))
            return Skip(log, "Microsoft Teams webhook URL is not configured.");

        var payload = new Dictionary<string, object>
        {
            ["@type"] = "MessageCard",
            ["@context"] = "https://schema.org/extensions",
            ["summary"] = "Alivert alert",
            ["themeColor"] = "22c55e",
            ["title"] = "Alivert alert",
            ["text"] = Truncate(message, 2900)
        };

        return await PostJsonAsync(log, uri, payload, "Microsoft Teams webhook", ct);
    }

    private async Task<AlertDeliveryLog> DeliverTelegramAsync(
        AlertDeliveryLog log,
        string? chatId,
        string message,
        CancellationToken ct)
    {
        var token = _options.CurrentValue.TelegramBotToken;
        if (string.IsNullOrWhiteSpace(token))
            return Skip(log, "Telegram bot token is not configured.");

        if (string.IsNullOrWhiteSpace(chatId))
            return Skip(log, "Telegram chat id is not configured.");

        var uri = new Uri($"https://api.telegram.org/bot{token.Trim()}/sendMessage");
        var payload = new
        {
            chat_id = chatId.Trim(),
            text = Truncate($"Alivert alert\n{message}", 3900),
            disable_web_page_preview = true
        };

        return await PostJsonAsync(log, uri, payload, $"Telegram chat {chatId.Trim()}", ct);
    }

    private async Task<AlertDeliveryLog> DeliverEmailAsync(
        AlertDeliveryLog log,
        Alert alert,
        MarketSnapshot snapshot,
        string message,
        UserNotificationSettings? settings,
        CancellationToken ct)
    {
        if (settings?.EmailEnabled == false)
            return Skip(log, "Email notifications are disabled.");

        var emailOptions = _options.CurrentValue.Email;
        if (!EmailTransportConfigured(emailOptions))
            return Skip(log, "Email transport is not configured yet.");

        var recipient = await _db.Users
            .AsNoTracking()
            .Where(user => user.Id == alert.UserId)
            .Select(user => user.Email)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(recipient))
            return Skip(log, "User email address is not configured.");

        using var mail = new MailMessage
        {
            From = new MailAddress(emailOptions.FromEmail!.Trim(), string.IsNullOrWhiteSpace(emailOptions.FromName) ? "Alivert" : emailOptions.FromName.Trim()),
            Subject = Truncate($"Alivert alert - {alert.Symbol}", 120),
            Body = BuildEmailBody(alert, snapshot, message),
            IsBodyHtml = false
        };
        mail.To.Add(recipient.Trim());

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.CurrentValue.RequestTimeoutSeconds, 1, 30)));

        using var client = new SmtpClient(emailOptions.SmtpServer.Trim(), emailOptions.SmtpPort)
        {
            EnableSsl = emailOptions.EnableSsl,
            Credentials = new NetworkCredential(
                string.IsNullOrWhiteSpace(emailOptions.Username) ? emailOptions.FromEmail!.Trim() : emailOptions.Username.Trim(),
                emailOptions.Password)
        };

        await client.SendMailAsync(mail, timeout.Token);

        log.Status = "Sent";
        log.Destination = recipient.Trim();
        return log;
    }

    private async Task<AlertDeliveryLog> PostJsonAsync(AlertDeliveryLog log, Uri uri, object payload, string destination, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.CurrentValue.RequestTimeoutSeconds, 1, 30)));

        using var response = await _httpClientFactory.CreateClient("notifications").PostAsJsonAsync(uri, payload, timeout.Token);
        log.Destination = destination;
        log.ResponseStatusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            log.Status = "Sent";
            return log;
        }

        log.Status = "Failed";
        var responseBody = await response.Content.ReadAsStringAsync(timeout.Token);
        log.ErrorMessage = Truncate(string.IsNullOrWhiteSpace(responseBody)
            ? $"HTTP {(int)response.StatusCode}"
            : responseBody, 500);
        return log;
    }

    private static AlertDeliveryLog Skip(AlertDeliveryLog log, string reason)
    {
        log.Status = "Skipped";
        log.ErrorMessage = Truncate(reason, 500);
        return log;
    }

    private static string? GetScheduleSkipReason(UserNotificationSettings? settings, DateTime utcNow)
    {
        if (settings?.AlertScheduleEnabled != true)
            return null;

        var zoneId = TimeZoneCatalog.ToSystemTimeZoneId(settings.AlertTimeZone);
        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            zone = TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            zone = TimeZoneInfo.Utc;
        }

        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, zone);
        var dayToken = localNow.DayOfWeek.ToString()[..3];
        var allowedDays = ParseDays(settings.AlertWindowDays);
        if (allowedDays.Count > 0 && !allowedDays.Contains(dayToken, StringComparer.OrdinalIgnoreCase))
            return "Outside alert schedule day.";

        if (!TimeSpan.TryParse(settings.AlertWindowStart, out var start) ||
            !TimeSpan.TryParse(settings.AlertWindowEnd, out var end))
            return null;

        var now = localNow.TimeOfDay;
        var insideWindow = start <= end
            ? now >= start && now <= end
            : now >= start || now <= end;

        return insideWindow ? null : "Outside alert schedule time window.";
    }

    private static HashSet<string> ParseDays(string? days)
    {
        if (string.IsNullOrWhiteSpace(days))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return days
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool TryAbsoluteHttpUri(string? url, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(url) ||
            !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var parsed) ||
            (parsed.Scheme != Uri.UriSchemeHttps && parsed.Scheme != Uri.UriSchemeHttp))
        {
            return false;
        }

        uri = parsed;
        return true;
    }

    private static bool EmailTransportConfigured(EmailNotificationOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.FromEmail) &&
            !string.IsNullOrWhiteSpace(options.Password) &&
            !string.IsNullOrWhiteSpace(options.SmtpServer) &&
            options.SmtpPort > 0;
    }

    private static string BuildEmailBody(Alert alert, MarketSnapshot snapshot, string message)
    {
        return string.Join(Environment.NewLine, new[]
        {
            "Alivert alert",
            "",
            message,
            "",
            $"Symbol: {alert.Symbol}",
            $"Rule: {alert.RuleType}",
            $"Timeframe: {alert.Timeframe}",
            $"Price: {snapshot.Price:0.########}",
            $"24h change: {snapshot.PercentChange24h:0.####}%",
            $"Checked at UTC: {snapshot.AsOfUtc:yyyy-MM-dd HH:mm:ss}"
        });
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
