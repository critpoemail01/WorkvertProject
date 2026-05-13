using Alivert.Data;
using Alivert.Models;
using Alivert.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Alivert.Pages.App;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserAccountService _accounts;

    public DashboardModel(ApplicationDbContext db, UserManager<IdentityUser> userManager, IUserAccountService accounts)
    {
        _db = db;
        _userManager = userManager;
        _accounts = accounts;
    }

    public int TotalAlerts { get; private set; }
    public int ActiveAlerts { get; private set; }
    public int UniqueSymbols { get; private set; }
    public int TriggersLast7Days { get; private set; }
    public int FailedDeliveriesLast7Days { get; private set; }
    public int UsersReached { get; private set; }
    public int UsersInteracted { get; private set; }
    public int UsersConverted { get; private set; }
    public int AiPlans { get; private set; }
    public int ScheduledAiItems { get; private set; }
    public int PublishedAiItems { get; private set; }
    public string ConversionRateLabel { get; private set; } = "0.0%";
    public bool IsUnlimitedPlan { get; private set; }
    public string PlanName { get; private set; } = "Basic (Free)";
    public string PlanDescription { get; private set; } = "Start with 5 free active campaign credits.";
    public string PlanCapacityLabel { get; private set; } = "5 active campaign slots";
    public string PlanRemainingLabel { get; private set; } = "5 remaining";
    public int PlanUsagePercent { get; private set; }
    public DateTime? PlanExpiresOnUtc { get; private set; }
    public int? PlanDaysRemaining { get; private set; }

    public List<TriggerRow> LatestTriggers { get; private set; } = new();
    public List<DeliveryRow> LatestDeliveries { get; private set; } = new();
    public List<AiPlanRow> LatestAiPlans { get; private set; } = new();

    public record TriggerRow(DateTime TriggeredAtUtc, string Symbol, string Message);
    public record DeliveryRow(DateTime CreatedAtUtc, string Symbol, string Channel, string Status, string? ErrorMessage);
    public record AiPlanRow(int Id, string ProductName, string Status, int Posts, int Emails, DateTime CreatedAtUtc);

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var limits = await _accounts.GetLimitsAsync(userId);

        TotalAlerts = await _db.Alerts.CountAsync(a => a.UserId == userId);
        ActiveAlerts = limits.ActiveAlerts;
        UniqueSymbols = await _db.Alerts.Where(a => a.UserId == userId).Select(a => a.Symbol).Distinct().CountAsync();

        var account = await _db.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        var latestPaidUnlimited = await _db.CreditPurchases
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == "Paid" && x.Credits == 0)
            .OrderByDescending(x => x.PaidAtUtc ?? x.CreatedAtUtc)
            .FirstOrDefaultAsync();

        LoadPlanSummary(limits, account, latestPaidUnlimited);

        var since = DateTime.UtcNow.AddDays(-7);
        TriggersLast7Days = await _db.AlertTriggers
            .AsNoTracking()
            .Where(t => t.Alert!.UserId == userId && t.TriggeredAtUtc >= since)
            .CountAsync();

        FailedDeliveriesLast7Days = await _db.AlertDeliveryLogs
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.CreatedAtUtc >= since && t.Status == "Failed")
            .CountAsync();

        var successfulDeliveriesLast7Days = await _db.AlertDeliveryLogs
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.CreatedAtUtc >= since && t.Status == "Sent")
            .CountAsync();

        var audienceLists = await _db.Alerts
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.IsEnabled && a.AudienceList != null)
            .Select(a => a.AudienceList!)
            .ToListAsync();
        var directAudienceContacts = audienceLists.Sum(CountAudienceContacts);

        AiPlans = await _db.MarketingPlans
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .CountAsync();

        ScheduledAiItems =
            await _db.MarketingPostSuggestions.AsNoTracking().CountAsync(x => x.MarketingPlan!.UserId == userId && x.Status == "Scheduled") +
            await _db.MarketingEmailSuggestions.AsNoTracking().CountAsync(x => x.MarketingPlan!.UserId == userId && x.Status == "Scheduled");

        PublishedAiItems =
            await _db.MarketingPostSuggestions.AsNoTracking().CountAsync(x => x.MarketingPlan!.UserId == userId && x.Status == "Published") +
            await _db.MarketingEmailSuggestions.AsNoTracking().CountAsync(x => x.MarketingPlan!.UserId == userId && x.Status == "Sent");

        var aiPostMetrics = await _db.MarketingPostSuggestions
            .AsNoTracking()
            .Where(x => x.MarketingPlan!.UserId == userId && x.Status == "Published")
            .Select(x => new { x.EstimatedReach, x.EstimatedInteractions, x.EstimatedConversions })
            .ToListAsync();

        var aiEmailMetrics = await _db.MarketingEmailSuggestions
            .AsNoTracking()
            .Where(x => x.MarketingPlan!.UserId == userId && x.Status == "Sent")
            .Select(x => new { x.EstimatedReach, x.EstimatedInteractions, x.EstimatedConversions })
            .ToListAsync();

        var aiReach = aiPostMetrics.Sum(x => x.EstimatedReach) + aiEmailMetrics.Sum(x => x.EstimatedReach);
        var aiInteractions = aiPostMetrics.Sum(x => x.EstimatedInteractions) + aiEmailMetrics.Sum(x => x.EstimatedInteractions);
        var aiConversions = aiPostMetrics.Sum(x => x.EstimatedConversions) + aiEmailMetrics.Sum(x => x.EstimatedConversions);

        UsersReached = (ActiveAlerts * 2500) + (successfulDeliveriesLast7Days * 350) + (TriggersLast7Days * 150) + directAudienceContacts + aiReach;
        UsersInteracted = UsersReached == 0 ? 0 : Math.Max(1, (int)Math.Round(UsersReached * 0.12m));
        UsersInteracted += aiInteractions;
        UsersConverted = UsersInteracted == 0 ? 0 : Math.Max(0, (int)Math.Round(UsersInteracted * 0.18m)) + aiConversions;
        ConversionRateLabel = UsersReached == 0
            ? "0.0%"
            : $"{(decimal)UsersConverted / UsersReached:P1}";

        LatestTriggers = await _db.AlertTriggers
            .AsNoTracking()
            .Where(t => t.Alert!.UserId == userId)
            .OrderByDescending(t => t.TriggeredAtUtc)
            .Take(20)
            .Select(t => new TriggerRow(t.TriggeredAtUtc, t.Alert!.Symbol, t.Message))
            .ToListAsync();

        LatestDeliveries = await _db.AlertDeliveryLogs
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(10)
            .Select(t => new DeliveryRow(t.CreatedAtUtc, t.Symbol, t.Channel, t.Status, t.ErrorMessage))
            .ToListAsync();

        LatestAiPlans = await _db.MarketingPlans
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(6)
            .Select(x => new AiPlanRow(x.Id, x.ProductName, x.Status, x.Posts.Count, x.Emails.Count, x.CreatedAtUtc))
            .ToListAsync();
    }

    private void LoadPlanSummary(
        (bool IsUnlimited, int Capacity, int ActiveAlerts, int RemainingSlots) limits,
        UserAccount? account,
        CreditPurchase? latestPaidUnlimited)
    {
        IsUnlimitedPlan = limits.IsUnlimited;
        PlanExpiresOnUtc = account?.UnlimitedUntilUtc;
        PlanDaysRemaining = PlanExpiresOnUtc is not null && PlanExpiresOnUtc.Value > DateTime.UtcNow
            ? Math.Max(0, (int)Math.Ceiling((PlanExpiresOnUtc.Value - DateTime.UtcNow).TotalDays))
            : null;

        if (IsUnlimitedPlan)
        {
            var isAnnual = latestPaidUnlimited?.PlanCode == "unlimited-annual" ||
                           latestPaidUnlimited?.SubscriptionDays >= 365 ||
                           PlanDaysRemaining > 300;

            PlanName = latestPaidUnlimited is null
                ? "Unlimited"
                : isAnnual
                    ? "Unlimited annual"
                    : "Unlimited monthly";
            PlanDescription = PlanDaysRemaining is null
                ? "Unlimited active campaigns are enabled."
                : $"Unlimited active campaigns until {PlanExpiresOnUtc!.Value.ToLocalTime():MMMM d, yyyy}.";
            PlanCapacityLabel = "Unlimited active campaigns";
            PlanRemainingLabel = PlanDaysRemaining is null
                ? "Active now"
                : $"{PlanDaysRemaining} day{(PlanDaysRemaining == 1 ? "" : "s")} remaining";
            PlanUsagePercent = 100;
            return;
        }

        PlanName = limits.Capacity <= 5 ? "Basic (Free)" : "Credit pack";
        PlanDescription = limits.Capacity <= 5
            ? "Free credits let you validate the campaign workflow before paying."
            : "Credit capacity is active. Add packs when you need more campaigns.";
        PlanCapacityLabel = $"{limits.Capacity} active campaign slot{(limits.Capacity == 1 ? "" : "s")}";
        PlanRemainingLabel = $"{limits.RemainingSlots} remaining";
        PlanUsagePercent = limits.Capacity <= 0
            ? 100
            : Math.Clamp((int)Math.Round((double)limits.ActiveAlerts / limits.Capacity * 100), 0, 100);
    }

    private static int CountAudienceContacts(string audienceList)
    {
        return audienceList
            .Split(new[] { '\r', '\n', ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Count(contact => !string.IsNullOrWhiteSpace(contact));
    }
}
