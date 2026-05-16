namespace Workvert.Models;

public record MarketSnapshot(
    string Symbol,
    decimal Price,
    decimal PercentChange24h,
    DateTime AsOfUtc,
    decimal? Volume24h = null,
    string? StoreName = null,
    string? ProductName = null,
    string? ProductUrl = null,
    decimal? ResaleBenchmarkPrice = null,
    decimal? OpportunityMarginPercent = null,
    string? Country = null,
    string? City = null,
    string? Category = null
);
