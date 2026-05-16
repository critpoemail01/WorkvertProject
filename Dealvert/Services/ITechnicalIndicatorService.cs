using Dealvert.Models;

namespace Dealvert.Services;

public interface ITechnicalIndicatorService
{
    Task<TechnicalIndicatorSnapshot?> GetSnapshotAsync(
        string symbol,
        MarketType marketType,
        string timeframe,
        int rsiPeriod,
        int fastEmaPeriod,
        int slowEmaPeriod,
        CancellationToken ct);
}
