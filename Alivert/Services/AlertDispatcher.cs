using Alivert.Data;
using Alivert.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
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
                "TELEGRAM" => await DeliverTelegramAsync(log, settings?.TelegramChatId, message, ct),
                "EMAIL" => Skip(log, settings?.EmailEnabled == false
                    ? "Email notifications are disabled."
                    : "Email transport is not configured yet."),
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

        var zoneId = MapTimeZoneId(string.IsNullOrWhiteSpace(settings.AlertTimeZone) ? "UTC" : settings.AlertTimeZone.Trim());
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

    private static string MapTimeZoneId(string zoneId)
    {
        return zoneId switch
        {
            "Europe/Lisbon" => OperatingSystem.IsWindows() ? "GMT Standard Time" : zoneId,
            _ => zoneId
        };
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

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
