using Dealvert.Models;

namespace Dealvert.Services;

public interface ISymbolCatalogService
{
    Task<IReadOnlyList<SymbolSearchResult>> SearchAsync(MarketType marketType, string query, CancellationToken ct);
    Task<bool> IsValidSymbolAsync(MarketType marketType, string symbol, CancellationToken ct);
}
