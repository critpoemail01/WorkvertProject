using Alivert.Data;
using Alivert.Models;
using Alivert.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Alivert.Pages.App;

[Authorize]
public class LogModel : PageModel
{
    private static readonly string[] SupportedTimeframes = ["1d", "3d", "1wk", "2wk", "1mo"];

    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserAccountService _accounts;

    public LogModel(ApplicationDbContext db, UserManager<IdentityUser> userManager, IUserAccountService accounts)
    {
        _db = db;
        _userManager = userManager;
        _accounts = accounts;
    }

    [BindProperty(SupportsGet = true)]
    public int? AlertId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Timeframe { get; set; }

    public IReadOnlyList<string> Timeframes => SupportedTimeframes;
    public List<AlertOption> AlertOptions { get; private set; } = new();
    public List<LogRow> Rows { get; private set; } = new();
    public List<RuleFilterRow> RuleFilters { get; private set; } = new();
    public string SelectedAlertName { get; private set; } = "All campaigns";
    public string SelectedTimeframe { get; private set; } = "all";
    public bool IsUnlimitedPlan { get; private set; }
    public string PlanCapacityLabel { get; private set; } = "5 active platform credits";

    public record AlertOption(int Id, string Label);
    public record LogRow(
        DateTime CreatedAtUtc,
        string Symbol,
        MarketType MarketType,
        AlertRuleType RuleType,
        string Timeframe,
        string Channel,
        string Status,
        string? Detail);

    public record RuleFilterRow(string Name, int Count, bool Enabled, string Tone);

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var limits = await _accounts.GetLimitsAsync(userId);
        IsUnlimitedPlan = limits.IsUnlimited;
        PlanCapacityLabel = limits.IsUnlimited
            ? "Unlimited active campaign-platforms"
            : $"{limits.ActiveAlerts}/{limits.Capacity} active platform credits used";

        var alerts = await _db.Alerts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.MarketType)
            .ThenBy(a => a.Symbol)
            .ThenBy(a => a.RuleType)
            .ToListAsync();

        AlertOptions = alerts
            .Select(a => new AlertOption(a.Id, $"{SourceTypeLabel(a.MarketType)}: {FormatSymbol(a.Symbol)} - {FormatRule(a.RuleType)}"))
            .ToList();

        if (AlertId is not null && alerts.All(a => a.Id != AlertId.Value))
            AlertId = null;

        if (AlertId is null && alerts.Count == 1)
            AlertId = alerts[0].Id;

        SelectedAlertName = AlertId is null
            ? "All campaigns"
            : AlertOptions.FirstOrDefault(x => x.Id == AlertId.Value)?.Label ?? "Selected campaign";

        SelectedTimeframe = NormalizeTimeframe(Timeframe);

        var query =
            from delivery in _db.AlertDeliveryLogs.AsNoTracking()
            join alert in _db.Alerts.AsNoTracking() on delivery.AlertId equals alert.Id
            where delivery.UserId == userId && alert.UserId == userId
            select new { delivery, alert };

        if (AlertId is not null)
            query = query.Where(x => x.alert.Id == AlertId.Value);

        if (SelectedTimeframe != "all")
            query = query.Where(x => x.alert.Timeframe == SelectedTimeframe);

        Rows = await query
            .OrderByDescending(x => x.delivery.CreatedAtUtc)
            .Take(100)
            .Select(x => new LogRow(
                x.delivery.CreatedAtUtc,
                x.alert.Symbol,
                x.alert.MarketType,
                x.alert.RuleType,
                x.alert.Timeframe,
                x.delivery.Channel,
                x.delivery.Status,
                x.delivery.ErrorMessage))
            .ToListAsync();

        RuleFilters = alerts
            .GroupBy(a => a.RuleType)
            .Select(g => new RuleFilterRow(
                FormatRule(g.Key),
                g.Count(),
                g.Any(a => a.IsEnabled),
                RuleTone(g.Key)))
            .OrderByDescending(x => x.Enabled)
            .ThenBy(x => x.Name)
            .ToList();
    }

    public string TimeframeUrl(string timeframe)
    {
        return Url.Page("/App/Log", new { alertId = AlertId, timeframe }) ?? "/App/Log";
    }

    public string AllTimeframesUrl()
    {
        return Url.Page("/App/Log", new { alertId = AlertId }) ?? "/App/Log";
    }

    public static string FormatRule(AlertRuleType ruleType)
    {
        return ruleType switch
        {
            _ => ruleType.DisplayName()
        };
    }

    public static string FormatSymbol(string symbol)
    {
        return symbol;
    }

    public static string SourceTypeLabel(MarketType marketType)
    {
        return marketType switch
        {
            MarketType.Crypto => "Application URL",
            MarketType.Traditional => "Company or idea",
            _ => "Campaign source"
        };
    }

    public static string CadenceLabel(string timeframe)
    {
        return timeframe switch
        {
            "1d" => "Daily",
            "3d" => "Every 3 days",
            "1wk" => "Weekly",
            "2wk" => "Biweekly",
            "1mo" => "Monthly",
            _ => timeframe
        };
    }

    private static string NormalizeTimeframe(string? timeframe)
    {
        if (string.IsNullOrWhiteSpace(timeframe))
            return "all";

        var normalized = timeframe.Trim().ToLowerInvariant();
        return SupportedTimeframes.Contains(normalized, StringComparer.OrdinalIgnoreCase) ? normalized : "all";
    }

    private static string RuleTone(AlertRuleType ruleType)
    {
        return ruleType switch
        {
            AlertRuleType.PriceAbove or AlertRuleType.RsiOversoldEmaCrossUp => "bull",
            AlertRuleType.PriceBelow or AlertRuleType.RsiOverboughtEmaCrossDown => "bear",
            AlertRuleType.RsiBelow or AlertRuleType.RsiAbove => "info",
            _ => "neutral"
        };
    }
}
