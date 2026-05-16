using Workvert.Models;

namespace Workvert.Services;

public interface IMarketDataService
{
    Task<MarketSnapshot> GetSnapshotAsync(string symbol, MarketType marketType, CancellationToken ct, Alert? alert = null);
}
