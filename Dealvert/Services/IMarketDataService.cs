using Dealvert.Models;

namespace Dealvert.Services;

public interface IMarketDataService
{
    Task<MarketSnapshot> GetSnapshotAsync(string symbol, MarketType marketType, CancellationToken ct, Alert? alert = null);
}
