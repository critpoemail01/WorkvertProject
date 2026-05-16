using Dealvert.Data;
using Dealvert.Models;
using Dealvert.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace Dealvert.Pages.App;

[Authorize]
public class BillingModel : PageModel
{
    private const string CreditCardProvider = "CreditCard";
    private const string StripeProvider = "Stripe";
    private static readonly string[] Providers = [CreditCardProvider];
    private const string CreditPlanCode = "credits";
    private const string MonthlyPlanCode = "unlimited-monthly";
    private const string AnnualPlanCode = "unlimited-annual";
    private const int MonthlySubscriptionDays = 30;
    private const int AnnualSubscriptionDays = 365;
    private const int CreditPackAccessDays = 30;

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
    public string PrimaryPaymentProvider => CreditCardProvider;
    public List<CreditPurchase> Purchases { get; private set; } = new();
    public List<CreditTransaction> Transactions { get; private set; } = new();
    public int CreditPackValidityDays => CreditPackAccessDays;
    public string FormatMoney(decimal amount, string currency) => $"{currency} {FormatAmount(amount)}";

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateCheckoutAsync(string checkoutKind, string provider, string? packId, string plan = "monthly")
    {
        await LoadAsync();

        if (string.Equals(checkoutKind, CreditPlanCode, StringComparison.OrdinalIgnoreCase))
            return await CreateCreditPackPurchaseAsync(packId, provider);

        if (string.Equals(checkoutKind, "unlimited", StringComparison.OrdinalIgnoreCase))
            return await CreateUnlimitedPurchaseAsync(provider, plan);

        ModelState.AddModelError(string.Empty, "Unsupported checkout selection.");
        return Page();
    }

    public async Task<IActionResult> OnPostAddCreditCardAsync(string provider)
    {
        await LoadAsync();

        if (!Providers.Contains(provider, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Unsupported payment provider.");
            return Page();
        }

        var reference = $"ALV-CARD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..36];
        var setupUrl = BuildCreditCardSetupUrl(provider, reference);
        if (!string.IsNullOrWhiteSpace(setupUrl))
            return Redirect(setupUrl);

        StatusMessage = "Credit card setup opened. Configure Payments:CreditCardSetupUrl to connect the hosted card setup flow.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreatePurchaseAsync(string packId, string provider)
    {
        await LoadAsync();
        return await CreateCreditPackPurchaseAsync(packId, provider);
    }

    public async Task<IActionResult> OnPostCreateUnlimitedAsync(string provider, string plan = "monthly")
    {
        await LoadAsync();
        return await CreateUnlimitedPurchaseAsync(provider, plan);
    }

    private async Task<IActionResult> CreateCreditPackPurchaseAsync(string? packId, string provider)
    {
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
            SubscriptionDays = CreditPackAccessDays,
            ExternalReference = reference,
            CheckoutUrl = checkoutUrl
        };

        _db.CreditPurchases.Add(purchase);
        await _db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(checkoutUrl))
            return Redirect(checkoutUrl);

        StatusMessage = $"Pending {provider} purchase created. Credit packs stay active for {CreditPackAccessDays} days after payment confirmation.";
        return RedirectToPage();
    }

    private async Task<IActionResult> CreateUnlimitedPurchaseAsync(string provider, string plan)
    {
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
            ? $"{purchase.Credits} credits added for {CreditPackAccessDays} days."
            : $"Unlimited alerts activated for {GetPurchaseUnlimitedPlan(purchase).DurationLabel}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemovePaidPlanAsync()
    {
        await LoadAsync();
        if (!CanManuallyConfirm)
            return Forbid();

        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var nowUtc = DateTime.UtcNow;
        var account = await _db.UserAccounts.FirstOrDefaultAsync(x => x.UserId == userId);
        var paidUnlimitedPurchases = await _db.CreditPurchases
            .Where(x => x.UserId == userId && x.Status == "Paid" && x.Credits == 0)
            .ToListAsync();

        var hadActivePlan = account?.UnlimitedUntilUtc is not null && account.UnlimitedUntilUtc.Value > nowUtc;

        if (account is not null && account.UnlimitedUntilUtc is not null)
        {
            account.UnlimitedUntilUtc = null;
            account.UpdatedAtUtc = nowUtc;
        }

        foreach (var purchase in paidUnlimitedPurchases)
        {
            purchase.Status = "TestRemoved";
            purchase.PaidAtUtc = null;
        }

        if (account is not null || paidUnlimitedPurchases.Count > 0)
            await _db.SaveChangesAsync();

        StatusMessage = hadActivePlan || paidUnlimitedPurchases.Count > 0
            ? "Paid Unlimited plan removed for testing. The account is back to Basic or credit-pack capacity."
            : "No active paid Unlimited plan was found.";

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
        CanManuallyConfirm = AllowsBillingTestActions();
        UnlimitedMonthlyAmount = _payments.CurrentValue.UnlimitedMonthlyAmount > 0 ? _payments.CurrentValue.UnlimitedMonthlyAmount : 49.99m;
        UnlimitedMonthlyCurrency = string.IsNullOrWhiteSpace(_payments.CurrentValue.UnlimitedMonthlyCurrency)
            ? "EUR"
            : _payments.CurrentValue.UnlimitedMonthlyCurrency;
        UnlimitedAnnualAmount = _payments.CurrentValue.UnlimitedAnnualAmount > 0 ? _payments.CurrentValue.UnlimitedAnnualAmount : 299;
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
        var isAnnualUnlimited = latestPaidUnlimited?.PlanCode == AnnualPlanCode ||
                                latestPaidUnlimited?.SubscriptionDays >= AnnualSubscriptionDays ||
                                DaysRemaining > 300;
        SubscriptionType = DaysRemaining is null
            ? "Basic (Free)"
            : latestPaidUnlimited is null
                ? "Unlimited"
                : isAnnualUnlimited
                    ? "Unlimited annual"
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
            return $"{purchase.Credits} credits / {GetCreditPurchaseDays(purchase)} days";

        return GetPurchaseUnlimitedPlan(purchase).Label;
    }

    public DateTime? GetCreditPurchaseExpiresOnUtc(CreditPurchase purchase)
    {
        if (purchase.Credits <= 0 || purchase.Status != "Paid")
            return null;

        return (purchase.PaidAtUtc ?? purchase.CreatedAtUtc).AddDays(GetCreditPurchaseDays(purchase));
    }

    public string GetPurchaseStatusLabel(CreditPurchase purchase)
    {
        var creditExpiresOnUtc = GetCreditPurchaseExpiresOnUtc(purchase);
        if (creditExpiresOnUtc is not null && creditExpiresOnUtc.Value <= DateTime.UtcNow)
            return "Expired";

        return purchase.Status;
    }

    public string GetPurchaseStatusBadgeClass(CreditPurchase purchase)
    {
        return GetPurchaseStatusLabel(purchase) switch
        {
            "Paid" => "text-bg-success",
            "Expired" => "text-bg-warning",
            _ => "text-bg-secondary"
        };
    }

    private string? BuildCheckoutUrl(string provider, string reference)
    {
        var url = ResolveCardProcessorUrl(provider);
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var separator = url.Contains('?') ? "&" : "?";
        return $"{url}{separator}reference={Uri.EscapeDataString(reference)}";
    }

    private string? BuildCreditCardSetupUrl(string provider, string reference)
    {
        var url = _payments.CurrentValue.CreditCardSetupUrl;
        if (string.IsNullOrWhiteSpace(url))
            url = ResolveCardProcessorUrl(provider);

        if (string.IsNullOrWhiteSpace(url))
            return null;

        var separator = url.Contains('?') ? "&" : "?";
        return $"{url}{separator}reference={Uri.EscapeDataString(reference)}&purpose=card_setup";
    }

    private string? ResolveCardProcessorUrl(string provider)
    {
        if (_payments.CurrentValue.ProviderCheckoutUrls.TryGetValue(provider, out var url) && !string.IsNullOrWhiteSpace(url))
            return url;

        if (string.Equals(provider, CreditCardProvider, StringComparison.OrdinalIgnoreCase) &&
            _payments.CurrentValue.ProviderCheckoutUrls.TryGetValue(StripeProvider, out url) &&
            !string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        return null;
    }

    private bool AllowsBillingTestActions()
    {
        var host = HttpContext.Request.Host.Host;
        var isLocalHost =
            string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);

        return isLocalHost || _environment.IsDevelopment() || _payments.CurrentValue.AllowManualPaymentConfirmation;
    }

    private static IReadOnlyList<CreditPackOptions> DefaultPacks()
    {
        return
        [
            new CreditPackOptions { Id = "starter", Credits = 25, Amount = 24.99m, Currency = "EUR" },
            new CreditPackOptions { Id = "growth", Credits = 50, Amount = 34.99m, Currency = "EUR" },
            new CreditPackOptions { Id = "pro", Credits = 100, Amount = 49.99m, Currency = "EUR" }
        ];
    }

    private static string FormatAmount(decimal amount)
    {
        return amount == decimal.Truncate(amount)
            ? amount.ToString("0", CultureInfo.InvariantCulture)
            : amount.ToString("0.00", CultureInfo.InvariantCulture);
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

    private static int GetCreditPurchaseDays(CreditPurchase purchase)
    {
        return purchase.SubscriptionDays > 0 ? purchase.SubscriptionDays : CreditPackAccessDays;
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
