namespace Alivert.Services;

public sealed class PaymentOptions
{
    public bool AllowManualPaymentConfirmation { get; set; }
    public decimal UnlimitedMonthlyAmount { get; set; } = 50;
    public string UnlimitedMonthlyCurrency { get; set; } = "EUR";
    public decimal UnlimitedAnnualAmount { get; set; } = 300;
    public string UnlimitedAnnualCurrency { get; set; } = "EUR";
    public List<CreditPackOptions> CreditPacks { get; set; } = new();
    public Dictionary<string, string> ProviderCheckoutUrls { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class CreditPackOptions
{
    public string Id { get; set; } = string.Empty;
    public int Credits { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
}
