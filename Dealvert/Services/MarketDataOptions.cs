namespace Dealvert.Services;

public sealed class MarketDataOptions
{
    public string Provider { get; set; } = "Fake";
    public string BinanceBaseUrl { get; set; } = "https://api.binance.com";
    public int RequestTimeoutSeconds { get; set; } = 8;
    public bool FallbackToFake { get; set; } = true;
}
