using Alivert.Data;
using Alivert.Models;
using Alivert.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Alivert.Pages.App;

[Authorize]
public class BillingModel : PageModel
{
    private static readonly string[] Providers = ["PayPal", "Stripe", "CoinbaseCommerce", "Crypto"];
    private const string CreditPlanCode = "credits";
    private const string MonthlyPlanCode = "unlimited-monthly";
    private const string AnnualPlanCode = "unlimited-annual";
    private const int MonthlySubscriptionDays = 30;
    private const int AnnualSubscriptionDays = 365;

    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserAccountService _accounts;
    private readonly IOptionsMonitor<PaymentOptions> _payments;
    private readonly IWebHostEnvironment _environment;

    public BillingModel(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
        IUserAccountService accounts,
        IOptionsMonitor<PaymentOptions> payments,
        IWebHostEnvironment environment)
    {
        _db = db;
        _userManager = userManager;
        _accounts = accounts;
        _payments = payments;
        _environment = environment;
    }

    public int Capacity { get; private set; }
    public int ActiveAlerts { get; private set; }
    public int RemainingSlots { get; private set; }
    public bool IsUnlimited { get; private set; }
    public bool CanManuallyConfirm { get; private set; }
    public decimal UnlimitedMonthlyAmount { get; private set; }
    public string UnlimitedMonthlyCurrency { get; private set; } = "EUR";
    public decimal UnlimitedAnnualAmount { get; private set; }
    public string UnlimitedAnnualCurrency { get; private set; } = "EUR";
    public string SubscriptionType { get; private set; } = "Basic (Free)";
    public DateTime PurchasedOnUtc { get; private set; }
    public DateTime? ExpiresOnUtc { get; private set; }
    public int? DaysRemaining { get; private set; }
    public IReadOnlyList<CreditPackOptions> Packs { get; private set; } = Array.Empty<CreditPackOptions>();
    public IReadOnlyList<string> PaymentProviders { get; private set; } = Providers;
    public List<CreditPurchase> Purchases { get; private set; } = new();
    public List<CreditTransaction> Transactions { get; private set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreatePurchaseAsync(string packId, string provider)
    {
        await LoadAsync();

        var pack = Packs.FirstOrDefault(x => string.Equals(x.Id, packId, StringComparison.OrdinalIgnoreCase));
        if (pack is null)
        {
            ModelState.AddModelError(string.Empty, "Unknown credit pack.");
            return Page();
        }

        if (!Providers.Contains(provider, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Unsupported payment provider.");
            return Page();
        }

        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var reference = $"ALV-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..30];
        var checkoutUrl = BuildCheckoutUrl(provider, reference);

        var purchase = new CreditPurchase
        {
            UserId = userId,
            Credits = pack.Credits,
            Amount = pack.Amount,
            Currency = pack.Currency,
            Provider = provider,
            Status = "Pending",
            PlanCode = CreditPlanCode,
            ExternalReference = reference,
            CheckoutUrl = checkoutUrl
        };

        _db.CreditPurchases.Add(purchase);
        await _db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(checkoutUrl))
            return Redirect(checkoutUrl);

        StatusMessage = $"Pending {provider} purchase created. Configure Payments:ProviderCheckoutUrls:{provider} to redirect users to checkout.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateUnlimitedAsync(string provider, string plan = "monthly")
    {
        await LoadAsync();

        if (!Providers.Contains(provider, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Unsupported payment provider.");
            return Page();
        }

        var selectedPlan = GetUnlimitedPlan(plan);
        if (selectedPlan is null)
        {
            ModelState.AddModelError(string.Empty, "Unsupported Unlimited plan.");
            return Page();
        }

        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var reference = $"{selectedPlan.ReferencePrefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        var checkoutUrl = BuildCheckoutUrl(provider, reference);

        var purchase = new CreditPurchase
        {
            UserId = userId,
            Credits = 0,
            Amount = selectedPlan.Amount,
            Currency = selectedPlan.Currency,
            Provider = provider,
            Status = "Pending",
            PlanCode = selectedPlan.Code,
            SubscriptionDays = selectedPlan.Days,
            ExternalReference = reference,
            CheckoutUrl = checkoutUrl
        };

        _db.CreditPurchases.Add(purchase);
        await _db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(checkoutUrl))
            return Redirect(checkoutUrl);

        StatusMessage = $"Pending {selectedPlan.Label} subscription created for {provider}. Configure Payments:ProviderCheckoutUrls:{provider} to redirect users to checkout.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkPaidAsync(int id)
    {
        await LoadAsync();
        if (!CanManuallyConfirm)
            return Forbid();

        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var purchase = await _db.CreditPurchases.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (purchase is null)
            return NotFound();

        if (purchase.Status != "Paid")
        {
            purchase.Status = "Paid";
            purchase.PaidAtUtc = DateTime.UtcNow;
            if (purchase.Credits > 0)
            {
                await _accounts.AddCreditsAsync(userId, purchase.Credits, "Credit purchase", purchase.ExternalReference);
            }
            else
            {
                var selectedPlan = GetPurchaseUnlimitedPlan(purchase);
                await _accounts.ActivateUnlimitedAsync(userId, TimeSpan.FromDays(selectedPlan.Days), selectedPlan.Reason, purchase.ExternalReference);
            }

            await _db.SaveChangesAsync();
        }

        StatusMessage = purchase.Credits > 0
            ? $"{purchase.Credits} credits added."
            : $"Unlimited alerts activated for {GetPurchaseUnlimitedPlan(purchase).DurationLabel}.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var limits = await _accounts.GetLimitsAsync(userId);
        Capacity = limits.Capacity;
        ActiveAlerts = limits.ActiveAlerts;
        RemainingSlots = limits.RemainingSlots;
        IsUnlimited = limits.IsUnlimited;
        CanManuallyConfirm = _environment.IsDevelopment() || _payments.CurrentValue.AllowManualPaymentConfirmation;
        UnlimitedMonthlyAmount = _payments.CurrentValue.UnlimitedMonthlyAmount > 0 ? _payments.CurrentValue.UnlimitedMonthlyAmount : 50;
        UnlimitedMonthlyCurrency = string.IsNullOrWhiteSpace(_payments.CurrentValue.UnlimitedMonthlyCurrency)
            ? "EUR"
            : _payments.CurrentValue.UnlimitedMonthlyCurrency;
        UnlimitedAnnualAmount = _payments.CurrentValue.UnlimitedAnnualAmount > 0 ? _payments.CurrentValue.UnlimitedAnnualAmount : 300;
        UnlimitedAnnualCurrency = string.IsNullOrWhiteSpace(_payments.CurrentValue.UnlimitedAnnualCurrency)
            ? "EUR"
            : _payments.CurrentValue.UnlimitedAnnualCurrency;
        Packs = _payments.CurrentValue.CreditPacks.Count > 0
            ? _payments.CurrentValue.CreditPacks
            : DefaultPacks();

        var account = await _db.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        var latestPaidUnlimited = await _db.CreditPurchases
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == "Paid" && x.Credits == 0)
            .OrderByDescending(x => x.PaidAtUtc ?? x.CreatedAtUtc)
            .FirstOrDefaultAsync();

        PurchasedOnUtc = latestPaidUnlimited?.PaidAtUtc ?? latestPaidUnlimited?.CreatedAtUtc ?? account?.CreatedAtUtc ?? DateTime.UtcNow;
        ExpiresOnUtc = account?.UnlimitedUntilUtc;
        DaysRemaining = ExpiresOnUtc is not null && ExpiresOnUtc.Value > DateTime.UtcNow
            ? Math.Max(0, (int)Math.Ceiling((ExpiresOnUtc.Value - DateTime.UtcNow).TotalDays))
            : null;
        SubscriptionType = DaysRemaining is null
            ? "Basic (Free)"
            : latestPaidUnlimited is null
                ? "Unlimited"
                : GetPurchasePlanLabel(latestPaidUnlimited);

        Purchases = await _db.CreditPurchases
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(20)
            .ToListAsync();

        Transactions = await _db.CreditTransactions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(20)
            .ToListAsync();
    }

    public string GetPurchasePlanLabel(CreditPurchase purchase)
    {
        if (purchase.Credits > 0)
            return $"{purchase.Credits} credits";

        return GetPurchaseUnlimitedPlan(purchase).Label;
    }

    private string? BuildCheckoutUrl(string provider, string reference)
    {
        if (!_payments.CurrentValue.ProviderCheckoutUrls.TryGetValue(provider, out var url) || string.IsNullOrWhiteSpace(url))
            return null;

        var separator = url.Contains('?') ? "&" : "?";
        return $"{url}{separator}reference={Uri.EscapeDataString(reference)}";
    }

    private static IReadOnlyList<CreditPackOptions> DefaultPacks()
    {
        return
        [
            new CreditPackOptions { Id = "starter", Credits = 25, Amount = 25, Currency = "EUR" },
            new CreditPackOptions { Id = "growth", Credits = 50, Amount = 35, Currency = "EUR" },
            new CreditPackOptions { Id = "pro", Credits = 100, Amount = 50, Currency = "EUR" }
        ];
    }

    private UnlimitedPlan? GetUnlimitedPlan(string? plan)
    {
        return string.Equals(plan, "annual", StringComparison.OrdinalIgnoreCase)
            ? new UnlimitedPlan(
                AnnualPlanCode,
                "Unlimited annual",
                UnlimitedAnnualAmount,
                UnlimitedAnnualCurrency,
                AnnualSubscriptionDays,
                "ALV-UNL-A",
                "Unlimited annual subscription",
                "1 year")
            : string.Equals(plan, "monthly", StringComparison.OrdinalIgnoreCase)
                ? new UnlimitedPlan(
                    MonthlyPlanCode,
                    "Unlimited monthly",
                    UnlimitedMonthlyAmount,
                    UnlimitedMonthlyCurrency,
                    MonthlySubscriptionDays,
                    "ALV-UNL-M",
                    "Unlimited monthly subscription",
                    "30 days")
                : null;
    }

    private UnlimitedPlan GetPurchaseUnlimitedPlan(CreditPurchase purchase)
    {
        return purchase.PlanCode == AnnualPlanCode || purchase.SubscriptionDays >= AnnualSubscriptionDays
            ? new UnlimitedPlan(
                AnnualPlanCode,
                "Unlimited annual",
                purchase.Amount,
                purchase.Currency,
                purchase.SubscriptionDays > 0 ? purchase.SubscriptionDays : AnnualSubscriptionDays,
                "ALV-UNL-A",
                "Unlimited annual subscription",
                "1 year")
            : new UnlimitedPlan(
                MonthlyPlanCode,
                "Unlimited monthly",
                purchase.Amount,
                purchase.Currency,
                purchase.SubscriptionDays > 0 ? purchase.SubscriptionDays : MonthlySubscriptionDays,
                "ALV-UNL-M",
                "Unlimited monthly subscription",
                "30 days");
    }

    private sealed record UnlimitedPlan(
        string Code,
        string Label,
        decimal Amount,
        string Currency,
        int Days,
        string ReferencePrefix,
        string Reason,
        string DurationLabel);
}
