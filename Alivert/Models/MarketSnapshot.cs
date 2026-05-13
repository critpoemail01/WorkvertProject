namespace Alivert.Models;

public record MarketSnapshot(
    string Symbol,
    decimal Price,
    decimal PercentChange24h,
    DateTime AsOfUtc,
    decimal? Volume24h = null
);
