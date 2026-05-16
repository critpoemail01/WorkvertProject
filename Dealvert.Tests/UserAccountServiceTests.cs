using Dealvert.Data;
using Dealvert.Models;
using Dealvert.Services;
using Microsoft.EntityFrameworkCore;

namespace Dealvert.Tests;

public class UserAccountServiceTests
{
    [Fact]
    public async Task EnsureAsync_CreatesAccountWithFreeCredits()
    {
        await using var db = CreateDbContext();
        var service = new UserAccountService(db);

        await service.EnsureAsync("user-1");

        var account = await db.UserAccounts.SingleAsync();
        Assert.Equal("user-1", account.UserId);
        Assert.Equal(5, account.Credits);
    }

    [Fact]
    public async Task GetLimitsAsync_CountsOnlyActiveProductAlerts()
    {
        await using var db = CreateDbContext();
        db.UserAccounts.Add(new UserAccount { UserId = "user-1", Credits = 5 });
        db.Alerts.AddRange(
            Alert("user-1", isEnabled: true),
            Alert("user-1", isEnabled: true),
            Alert("user-1", isEnabled: false),
            Alert("other-user", isEnabled: true));
        db.MarketingPlans.AddRange(
            MarketingPlan("user-1", "Scheduled", "TikTok,Instagram,Instagram,Facebook"),
            MarketingPlan("user-1", "Draft", "LinkedIn,X"),
            MarketingPlan("other-user", "Scheduled", "TikTok,Instagram"));
        await db.SaveChangesAsync();

        var service = new UserAccountService(db);
        var limits = await service.GetLimitsAsync("user-1");

        Assert.False(limits.IsUnlimited);
        Assert.Equal(5, limits.Capacity);
        Assert.Equal(2, limits.ActiveAlerts);
        Assert.Equal(3, limits.RemainingSlots);
    }

    [Fact]
    public async Task GetLimitsAsync_ReturnsUnlimited_WhenUnlimitedUntilIsInFuture()
    {
        await using var db = CreateDbContext();
        db.UserAccounts.Add(new UserAccount
        {
            UserId = "user-1",
            Credits = 0,
            UnlimitedUntilUtc = DateTime.UtcNow.AddDays(1)
        });
        await db.SaveChangesAsync();

        var service = new UserAccountService(db);
        var limits = await service.GetLimitsAsync("user-1");

        Assert.True(limits.IsUnlimited);
        Assert.Equal(int.MaxValue, limits.Capacity);
        Assert.Equal(int.MaxValue, limits.RemainingSlots);
    }

    [Fact]
    public async Task AddCreditsAsync_IncrementsAccountAndIsIdempotentByReference()
    {
        await using var db = CreateDbContext();
        var service = new UserAccountService(db);

        await service.AddCreditsAsync("user-1", 25, "Credit purchase", "payment-1");
        await service.AddCreditsAsync("user-1", 25, "Credit purchase", "payment-1");

        var account = await db.UserAccounts.SingleAsync();
        var transaction = await db.CreditTransactions.SingleAsync();

        Assert.Equal(30, account.Credits);
        Assert.Equal(25, transaction.Delta);
        Assert.Equal("payment-1", transaction.Reference);
    }

    [Fact]
    public async Task GetLimitsAsync_IncludesPaidCreditsOnlyForThirtyDays()
    {
        await using var db = CreateDbContext();
        db.UserAccounts.Add(new UserAccount { UserId = "user-1", Credits = 30 });
        db.CreditTransactions.Add(new CreditTransaction
        {
            UserId = "user-1",
            Delta = 25,
            Reason = "Credit purchase",
            Reference = "active-credit",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-29)
        });
        await db.SaveChangesAsync();

        var service = new UserAccountService(db);
        var limits = await service.GetLimitsAsync("user-1");

        Assert.False(limits.IsUnlimited);
        Assert.Equal(30, limits.Capacity);
        Assert.Equal(30, limits.RemainingSlots);
    }

    [Fact]
    public async Task GetLimitsAsync_ExcludesExpiredPaidCredits()
    {
        await using var db = CreateDbContext();
        db.UserAccounts.Add(new UserAccount { UserId = "user-1", Credits = 30 });
        db.CreditTransactions.Add(new CreditTransaction
        {
            UserId = "user-1",
            Delta = 25,
            Reason = "Credit purchase",
            Reference = "expired-credit",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-31)
        });
        await db.SaveChangesAsync();

        var service = new UserAccountService(db);
        var limits = await service.GetLimitsAsync("user-1");

        Assert.False(limits.IsUnlimited);
        Assert.Equal(5, limits.Capacity);
        Assert.Equal(5, limits.RemainingSlots);
    }

    [Fact]
    public async Task GetLimitsAsync_DisablesAlertsAboveExpiredCreditCapacity()
    {
        await using var db = CreateDbContext();
        db.UserAccounts.Add(new UserAccount { UserId = "user-1", Credits = 30 });
        db.CreditTransactions.Add(new CreditTransaction
        {
            UserId = "user-1",
            Delta = 25,
            Reason = "Credit purchase",
            Reference = "expired-credit",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-31)
        });
        db.Alerts.AddRange(Enumerable.Range(0, 8).Select(_ => Alert("user-1", isEnabled: true)));
        await db.SaveChangesAsync();

        var service = new UserAccountService(db);
        var limits = await service.GetLimitsAsync("user-1");
        var enabledAlerts = await db.Alerts.CountAsync(a => a.UserId == "user-1" && a.IsEnabled);

        Assert.False(limits.IsUnlimited);
        Assert.Equal(5, limits.Capacity);
        Assert.Equal(5, limits.ActiveAlerts);
        Assert.Equal(0, limits.RemainingSlots);
        Assert.Equal(5, enabledAlerts);
    }

    [Fact]
    public async Task GetLimitsAsync_IgnoresLegacyScheduledPlans()
    {
        await using var db = CreateDbContext();
        db.UserAccounts.Add(new UserAccount { UserId = "user-1", Credits = 5 });
        var plan = MarketingPlan("user-1", "Scheduled", "TikTok,Instagram,Facebook,LinkedIn");
        db.MarketingPlans.Add(plan);
        db.Alerts.AddRange(Enumerable.Range(0, 3).Select(_ => Alert("user-1", isEnabled: true)));
        await db.SaveChangesAsync();

        var service = new UserAccountService(db);
        var limits = await service.GetLimitsAsync("user-1");
        var enabledAlerts = await db.Alerts.CountAsync(a => a.UserId == "user-1" && a.IsEnabled);
        var savedPlan = await db.MarketingPlans.SingleAsync(x => x.UserId == "user-1");

        Assert.False(limits.IsUnlimited);
        Assert.Equal(5, limits.Capacity);
        Assert.Equal(3, limits.ActiveAlerts);
        Assert.Equal(2, limits.RemainingSlots);
        Assert.Equal(3, enabledAlerts);
        Assert.Equal("Scheduled", savedPlan.Status);
    }

    [Fact]
    public async Task GetLimitsAsync_DoesNotPauseLegacyScheduledPlans()
    {
        await using var db = CreateDbContext();
        db.UserAccounts.Add(new UserAccount { UserId = "user-1", Credits = 5 });
        var plan = MarketingPlan("user-1", "Scheduled", "TikTok,Instagram,Facebook,LinkedIn,X,YouTube Shorts");
        plan.Posts.Add(new MarketingPostSuggestion
        {
            Platform = "TikTok",
            ScheduledForUtc = DateTime.UtcNow.AddHours(1),
            DayNumber = 1,
            Title = "Launch hook",
            Hook = "Show the product",
            Caption = "Try it today",
            CreativeBrief = "Short product demo",
            CallToAction = "Subscribe",
            Status = "Scheduled"
        });
        plan.Emails.Add(new MarketingEmailSuggestion
        {
            ScheduledForUtc = DateTime.UtcNow.AddHours(2),
            DayNumber = 1,
            Subject = "Launch",
            PreviewText = "New workflow",
            Body = "Try it today",
            AudienceSegment = "Prospects",
            Status = "Scheduled"
        });
        db.MarketingPlans.Add(plan);
        await db.SaveChangesAsync();

        var service = new UserAccountService(db);
        var limits = await service.GetLimitsAsync("user-1");

        var savedPlan = await db.MarketingPlans
            .Include(x => x.Posts)
            .Include(x => x.Emails)
            .SingleAsync(x => x.UserId == "user-1");

        Assert.False(limits.IsUnlimited);
        Assert.Equal(5, limits.Capacity);
        Assert.Equal(0, limits.ActiveAlerts);
        Assert.Equal(5, limits.RemainingSlots);
        Assert.Equal("Scheduled", savedPlan.Status);
        Assert.Equal("Scheduled", savedPlan.Posts.Single().Status);
        Assert.Equal("Scheduled", savedPlan.Emails.Single().Status);
    }

    [Fact]
    public async Task ActivateUnlimitedAsync_ExtendsUnlimitedAndIsIdempotentByReference()
    {
        await using var db = CreateDbContext();
        var service = new UserAccountService(db);

        await service.ActivateUnlimitedAsync("user-1", TimeSpan.FromDays(30), "Unlimited monthly subscription", "sub-1");
        var firstUntil = await db.UserAccounts.Select(x => x.UnlimitedUntilUtc).SingleAsync();
        await service.ActivateUnlimitedAsync("user-1", TimeSpan.FromDays(30), "Unlimited monthly subscription", "sub-1");

        var account = await db.UserAccounts.SingleAsync();
        var transaction = await db.CreditTransactions.SingleAsync();

        Assert.NotNull(account.UnlimitedUntilUtc);
        Assert.Equal(firstUntil, account.UnlimitedUntilUtc);
        Assert.Equal(0, transaction.Delta);
        Assert.Equal("sub-1", transaction.Reference);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Alert Alert(string userId, bool isEnabled)
    {
        return new Alert
        {
            UserId = userId,
            Symbol = "BTCUSDT",
            RuleType = AlertRuleType.PriceAbove,
            Threshold = 100,
            IsEnabled = isEnabled
        };
    }

    private static MarketingPlan MarketingPlan(string userId, string status, string platforms)
    {
        return new MarketingPlan
        {
            UserId = userId,
            ProductName = "SBI Flow",
            ProductUrl = "https://example.com",
            CompanyOrIdea = "Operations software",
            TargetAudience = "Industrial SMEs",
            ValueProposition = "Reduce manual planning work",
            CampaignGoal = "subscriptions",
            Tone = "clear",
            Platforms = platforms,
            Frequency = "Daily",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Status = status
        };
    }
}
