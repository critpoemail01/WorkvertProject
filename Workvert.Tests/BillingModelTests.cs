using System.Security.Claims;
using Workvert.Data;
using Workvert.Models;
using Workvert.Pages.App;
using Workvert.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Workvert.Tests;

public class BillingModelTests
{
    [Fact]
    public async Task CreateCheckoutAsync_CreatesPendingCreditCardCreditPurchase()
    {
        await using var db = CreateDbContext();
        var model = CreateModel(db);

        var result = await model.OnPostCreateCheckoutAsync("credits", "CreditCard", "starter");

        var purchase = await db.CreditPurchases.SingleAsync();
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("user-1", purchase.UserId);
        Assert.Equal("CreditCard", purchase.Provider);
        Assert.Equal("Pending", purchase.Status);
        Assert.Equal("credits", purchase.PlanCode);
        Assert.Equal(25, purchase.Credits);
        Assert.Equal(30, purchase.SubscriptionDays);
    }

    [Fact]
    public async Task CreateCheckoutAsync_RejectsAlternativePaymentProvider()
    {
        await using var db = CreateDbContext();
        var model = CreateModel(db);

        var result = await model.OnPostCreateCheckoutAsync("credits", "PayPal", "starter");

        Assert.IsType<PageResult>(result);
        Assert.True(model.ModelState.ErrorCount > 0);
        Assert.Empty(await db.CreditPurchases.ToListAsync());
    }

    [Fact]
    public async Task AddCreditCardAsync_DoesNotCreatePurchaseWhenSetupUrlIsMissing()
    {
        await using var db = CreateDbContext();
        var model = CreateModel(db);

        var result = await model.OnPostAddCreditCardAsync("CreditCard");

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Empty(await db.CreditPurchases.ToListAsync());
        Assert.Contains("Credit card setup opened", model.StatusMessage);
    }

    [Fact]
    public async Task AddCreditCardAsync_RedirectsToConfiguredCardSetupUrl()
    {
        await using var db = CreateDbContext();
        var model = CreateModel(db, options =>
        {
            options.CreditCardSetupUrl = "https://payments.example/setup-card";
        });

        var result = await model.OnPostAddCreditCardAsync("CreditCard");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.StartsWith("https://payments.example/setup-card?reference=ALV-CARD-", redirect.Url);
        Assert.Contains("&purpose=card_setup", redirect.Url);
        Assert.Empty(await db.CreditPurchases.ToListAsync());
    }

    private static BillingModel CreateModel(ApplicationDbContext db, Action<PaymentOptions>? configure = null)
    {
        db.UserAccounts.Add(new UserAccount { UserId = "user-1", Credits = 5 });
        db.SaveChanges();

        var payments = new PaymentOptions
        {
            UnlimitedMonthlyAmount = 50,
            UnlimitedMonthlyCurrency = "EUR",
            UnlimitedAnnualAmount = 300,
            UnlimitedAnnualCurrency = "EUR",
            CreditPacks =
            [
                new CreditPackOptions { Id = "starter", Credits = 25, Amount = 25, Currency = "EUR" },
                new CreditPackOptions { Id = "growth", Credits = 50, Amount = 35, Currency = "EUR" }
            ],
            ProviderCheckoutUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };
        configure?.Invoke(payments);

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "user-1")
            ], "test"))
        };
        httpContext.Request.Host = new HostString("localhost");

        return new BillingModel(
            db,
            CreateUserManager(db),
            new UserAccountService(db),
            new TestOptionsMonitor<PaymentOptions>(payments),
            new TestWebHostEnvironment())
        {
            PageContext = new PageContext { HttpContext = httpContext }
        };
    }

    private static UserManager<IdentityUser> CreateUserManager(ApplicationDbContext db)
    {
        var store = new UserStore<IdentityUser>(db);
        return new UserManager<IdentityUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<IdentityUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<IdentityUser>>.Instance);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public TestOptionsMonitor(T currentValue)
        {
            CurrentValue = currentValue;
        }

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Workvert.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
