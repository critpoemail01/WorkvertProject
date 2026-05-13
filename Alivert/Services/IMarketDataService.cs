using Alivert.Models;

namespace Alivert.Services;

public interface IMarketDataService
{
    Task<MarketSnapshot> GetSnapshotAsync(string symbol, MarketType marketType, CancellationToken ct);
}
