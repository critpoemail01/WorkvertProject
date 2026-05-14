using Alivert.Data;
using Alivert.Models;
using Alivert.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alivert.ViewComponents;

public sealed class AppPlanBadgeViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserAccountService _accounts;

    public AppPlanBadgeViewComponent(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
        IUserAccountService accounts)
    {
        _db = db;
        _userManager = userManager;
        _accounts = accounts;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = _userManager.GetUserId(ViewContext.HttpContext.User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Content(string.Empty);
        }

        var cancellationToken = ViewContext.HttpContext.RequestAborted;
        var limits = await _accounts.GetLimitsAsync(userId, cancellationToken);

        var account = await _db.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        var latestPaidUnlimited = await _db.CreditPurchases
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == "Paid" && x.Credits == 0)
            .OrderByDescending(x => x.PaidAtUtc ?? x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return View(BuildModel(limits, account, latestPaidUnlimited));
    }

    private static AppPlanBadgeModel BuildModel(
        (bool IsUnlimited, int Capacity, int ActiveAlerts, int RemainingSlots) limits,
        UserAccount? account,
        CreditPurchase? latestPaidUnlimited)
    {
        if (limits.IsUnlimited)
        {
            var expiresOnUtc = account?.UnlimitedUntilUtc;
            var daysRemaining = expiresOnUtc is not null && expiresOnUtc.Value > DateTime.UtcNow
                ? Math.Max(0, (int)Math.Ceiling((expiresOnUtc.Value - DateTime.UtcNow).TotalDays))
                : (int?)null;
            var isAnnual = latestPaidUnlimited?.PlanCode == "unlimited-annual" ||
                           latestPaidUnlimited?.SubscriptionDays >= 365 ||
                           daysRemaining > 300;

            var planName = latestPaidUnlimited is null
                ? "Unlimited"
                : isAnnual
                    ? "Unlimited annual"
                    : "Unlimited monthly";

            var meta = daysRemaining is null
                ? "Active now"
                : $"{daysRemaining} day{(daysRemaining == 1 ? string.Empty : "s")} left";

            return new AppPlanBadgeModel(planName, meta, "bi-stars", "unlimited");
        }

        var capacity = Math.Max(0, limits.Capacity);
        var remaining = Math.Max(0, limits.RemainingSlots);
        var isFree = capacity <= 5;
        var name = isFree ? "Basic (Free)" : "Credit pack";
        var metaText = isFree
            ? $"{capacity} free platform credits"
            : $"{remaining} of {capacity} platform credits free";

        return new AppPlanBadgeModel(name, metaText, isFree ? "bi-wallet2" : "bi-credit-card-2-front", isFree ? "basic" : "credits");
    }

    public sealed record AppPlanBadgeModel(string Name, string Meta, string IconClass, string Variant);
}
