using Alivert.Data;
using Alivert.Models;
using Alivert.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Alivert.Pages.App.Alerts;

[Authorize]
public class AlertsIndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserAccountService _accounts;

    public AlertsIndexModel(ApplicationDbContext db, UserManager<IdentityUser> userManager, IUserAccountService accounts)
    {
        _db = db;
        _userManager = userManager;
        _accounts = accounts;
    }

    public List<Alert> Alerts { get; private set; } = new();
    public List<AlertGroup> AlertGroups { get; private set; } = new();
    public UserNotificationSettings? ScheduleSettings { get; private set; }
    public IReadOnlyList<ScheduleTimeZoneChoice> ScheduleTimeZones { get; private set; } = TimeZoneCatalog.GetScheduleChoices(DateTime.UtcNow);
    public string SelectedScheduleTimeZone { get; private set; } = TimeZoneCatalog.DefaultTimeZoneId;
    public bool IsUnlimitedPlan { get; private set; }
    public bool IsAnnualUnlimitedPlan { get; private set; }
    public string PlanPromoTitle { get; private set; } = "Unlock all features & timeframes";
    public string PlanPromoText { get; private set; } = "Unlimited yearly is EUR 300 and covers every setup without counting credits.";

    [TempData]
    public string? StatusMessage { get; set; }

    public record AlertGroup(MarketType MarketType, string Symbol, List<Alert> Alerts, List<AlertTriggerHistory> TriggerHistory);
    public record AlertTriggerHistory(int AlertId, AlertRuleType RuleType, decimal Threshold, string Timeframe, DateTime TriggeredAtUtc, string Message);

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostToggleAlertAsync(int id)
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var alert = await _db.Alerts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (alert is null) return NotFound();

        if (!alert.IsEnabled)
        {
            var limits = await _accounts.GetLimitsAsync(userId);
            if (!limits.IsUnlimited && limits.RemainingSlots <= 0)
            {
                StatusMessage = "No credits available. Disable another alert or upgrade your plan before turning this alert on.";
                return RedirectToPage();
            }
        }

        alert.IsEnabled = !alert.IsEnabled;
        alert.IndicatorArmed = false;
        alert.LastIndicatorValue = null;
        alert.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        StatusMessage = alert.IsEnabled ? "Alert turned on." : "Alert turned off.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAlertAsync(int id)
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var alert = await _db.Alerts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (alert is null) return NotFound();

        await DeleteAlertsAsync([alert]);
        StatusMessage = "Alert deleted.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleGroupAsync(MarketType marketType, string symbol)
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var alerts = await LoadOwnedGroupAsync(userId, marketType, symbol);
        if (alerts.Count == 0) return NotFound();

        var shouldEnable = alerts.All(a => !a.IsEnabled);
        if (shouldEnable)
        {
            var limits = await _accounts.GetLimitsAsync(userId);
            var alertsToEnable = alerts.Count(a => !a.IsEnabled);
            if (!limits.IsUnlimited && alertsToEnable > limits.RemainingSlots)
            {
                StatusMessage = $"No credits available. This group needs {alertsToEnable} slot(s), but only {limits.RemainingSlots} remain.";
                return RedirectToPage();
            }
        }

        foreach (var alert in alerts)
        {
            alert.IsEnabled = shouldEnable;
            alert.IndicatorArmed = false;
            alert.LastIndicatorValue = null;
            alert.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        StatusMessage = shouldEnable ? "Alert group turned on." : "Alert group turned off.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteGroupAsync(MarketType marketType, string symbol)
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var alerts = await LoadOwnedGroupAsync(userId, marketType, symbol);
        if (alerts.Count == 0) return NotFound();

        await DeleteAlertsAsync(alerts);
        StatusMessage = "Alert group deleted.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostScheduleAsync(string startTime, string endTime, string timeZone, string[] days)
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;

        if (!TimeSpan.TryParse(startTime, out _) || !TimeSpan.TryParse(endTime, out _))
        {
            StatusMessage = "Use valid start and end times for the schedule.";
            return RedirectToPage();
        }

        var settings = await _db.UserNotificationSettings.FirstOrDefaultAsync(x => x.UserId == userId);
        if (settings is null)
        {
            settings = new UserNotificationSettings { UserId = userId };
            _db.UserNotificationSettings.Add(settings);
        }

        settings.AlertScheduleEnabled = true;
        settings.AlertWindowStart = NormalizeTime(startTime);
        settings.AlertWindowEnd = NormalizeTime(endTime);
        settings.AlertTimeZone = TimeZoneCatalog.Normalize(timeZone);
        settings.AlertWindowDays = NormalizeDays(days);
        await _db.SaveChangesAsync();

        StatusMessage = "Alert schedule saved. Notifications outside this window will be skipped.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveScheduleAsync()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var settings = await _db.UserNotificationSettings.FirstOrDefaultAsync(x => x.UserId == userId);
        if (settings is not null)
        {
            settings.AlertScheduleEnabled = false;
            settings.AlertWindowStart = null;
            settings.AlertWindowEnd = null;
            settings.AlertWindowDays = null;
            await _db.SaveChangesAsync();
        }

        StatusMessage = "24/7 alert delivery restored.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;

        Alerts = await _db.Alerts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsEnabled)
            .ThenBy(a => a.Symbol)
            .ToListAsync();

        var triggerRows = await (
            from trigger in _db.AlertTriggers.AsNoTracking()
            join alert in _db.Alerts.AsNoTracking() on trigger.AlertId equals alert.Id
            where alert.UserId == userId
            orderby trigger.TriggeredAtUtc descending
            select new
            {
                alert.MarketType,
                alert.Symbol,
                AlertId = alert.Id,
                alert.RuleType,
                alert.Threshold,
                alert.Timeframe,
                trigger.TriggeredAtUtc,
                trigger.Message
            })
            .Take(200)
            .ToListAsync();

        var triggerHistoryByGroup = triggerRows
            .GroupBy(x => (x.MarketType, x.Symbol))
            .ToDictionary(
                g => g.Key,
                g => g.Take(10)
                    .Select(x => new AlertTriggerHistory(x.AlertId, x.RuleType, x.Threshold, x.Timeframe, x.TriggeredAtUtc, x.Message))
                    .ToList());

        AlertGroups = Alerts
            .GroupBy(a => new { a.MarketType, a.Symbol })
            .Select(g =>
            {
                var key = (g.Key.MarketType, g.Key.Symbol);
                return new AlertGroup(
                    g.Key.MarketType,
                    g.Key.Symbol,
                    g.ToList(),
                    triggerHistoryByGroup.TryGetValue(key, out var history) ? history : new List<AlertTriggerHistory>());
            })
            .OrderBy(g => g.MarketType)
            .ThenBy(g => g.Symbol)
            .ToList();

        ScheduleSettings = await _db.UserNotificationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        SelectedScheduleTimeZone = TimeZoneCatalog.Normalize(ScheduleSettings?.AlertTimeZone);
        var nowUtc = DateTime.UtcNow;
        ScheduleTimeZones = TimeZoneCatalog.GetScheduleChoices(nowUtc);

        var account = await _db.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        var latestPaidUnlimited = await _db.CreditPurchases
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == "Paid" && x.Credits == 0)
            .OrderByDescending(x => x.PaidAtUtc ?? x.CreatedAtUtc)
            .FirstOrDefaultAsync();

        LoadPlanPromo(account?.UnlimitedUntilUtc is not null && account.UnlimitedUntilUtc.Value > nowUtc, account, latestPaidUnlimited, nowUtc);
    }

    private async Task<List<Alert>> LoadOwnedGroupAsync(string userId, MarketType marketType, string symbol)
    {
        var normalizedSymbol = (symbol ?? string.Empty).Trim().ToUpperInvariant();
        return await _db.Alerts
            .Where(a => a.UserId == userId && a.MarketType == marketType && a.Symbol == normalizedSymbol)
            .ToListAsync();
    }

    private async Task DeleteAlertsAsync(List<Alert> alerts)
    {
        var ids = alerts.Select(a => a.Id).ToArray();
        var triggers = await _db.AlertTriggers.Where(t => ids.Contains(t.AlertId)).ToListAsync();
        _db.AlertTriggers.RemoveRange(triggers);
        _db.Alerts.RemoveRange(alerts);
        await _db.SaveChangesAsync();
    }

    private static string NormalizeTime(string value)
    {
        return TimeSpan.TryParse(value, out var parsed)
            ? parsed.ToString(@"hh\:mm")
            : "00:00";
    }

    private static string NormalizeDays(string[] days)
    {
        var allowed = new HashSet<string>(["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"], StringComparer.OrdinalIgnoreCase);
        var selected = days.Where(allowed.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return selected.Length == 0 ? "Mon,Tue,Wed,Thu,Fri,Sat,Sun" : string.Join(",", selected);
    }

    private void LoadPlanPromo(bool isUnlimited, UserAccount? account, CreditPurchase? latestPaidUnlimited, DateTime nowUtc)
    {
        IsUnlimitedPlan = isUnlimited;
        IsAnnualUnlimitedPlan = isUnlimited &&
            (latestPaidUnlimited?.PlanCode == "unlimited-annual" ||
             latestPaidUnlimited?.SubscriptionDays >= 365 ||
             (account?.UnlimitedUntilUtc is not null && (account.UnlimitedUntilUtc.Value - nowUtc).TotalDays > 300));

        if (IsAnnualUnlimitedPlan)
        {
            PlanPromoTitle = "Annual unlimited active";
            PlanPromoText = account?.UnlimitedUntilUtc is null
                ? "Every alert type, preset and timeframe is already included in your annual account."
                : $"Every alert type, preset and timeframe is included until {account.UnlimitedUntilUtc.Value.ToLocalTime():MMM d, yyyy}.";
            return;
        }

        if (IsUnlimitedPlan)
        {
            PlanPromoTitle = "Switch to annual and save";
            PlanPromoText = "You already have unlimited alerts. Annual billing is EUR 300/year and saves EUR 300 versus paying monthly.";
        }
    }
}
