using Alivert.Models;
using System.Collections.Concurrent;

namespace Alivert.Services;

/// <summary>
/// MVP provider: generates a stable-ish random walk per symbol.
/// Replace with a real market-data provider later.
/// </summary>
public sealed class FakeMarketDataService : IMarketDataService
{
    private readonly ConcurrentDictionary<string, decimal> _lastPrice = new(StringComparer.OrdinalIgnoreCase);

    public Task<MarketSnapshot> GetSnapshotAsync(string symbol, CancellationToken ct)
    {
        var price = _lastPrice.GetOrAdd(symbol, _ => (decimal)(50 + Random.Shared.NextDouble() * 200));

        // random walk
        var step = (decimal)((Random.Shared.NextDouble() - 0.5) * 2.0); // -1..1
        price = Math.Max(0.01m, price + step);
        _lastPrice[symbol] = price;

        var pct24h = (decimal)((Random.Shared.NextDouble() - 0.5) * 10.0); // -5..5

        return Task.FromResult(new MarketSnapshot(symbol, decimal.Round(price, 2), decimal.Round(pct24h, 2), DateTime.UtcNow));
    }
}
