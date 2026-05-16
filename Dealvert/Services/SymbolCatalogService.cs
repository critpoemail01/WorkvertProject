using System.Collections.Concurrent;
using System.Text.Json;
using Dealvert.Models;
using Microsoft.Extensions.Options;

namespace Dealvert.Services;

public sealed class SymbolCatalogService : ISymbolCatalogService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<MarketDataOptions> _options;
    private readonly ILogger<SymbolCatalogService> _logger;
    private readonly SemaphoreSlim _binanceLock = new(1, 1);
    private IReadOnlyList<SymbolSearchResult>? _binanceSymbols;
    private DateTime _binanceLoadedAtUtc;

    public SymbolCatalogService(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<MarketDataOptions> options,
        ILogger<SymbolCatalogService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SymbolSearchResult>> SearchAsync(MarketType marketType, string query, CancellationToken ct)
    {
        query = (query ?? string.Empty).Trim();
        if (query.Length < 1)
            return Array.Empty<SymbolSearchResult>();

        return marketType == MarketType.Crypto
            ? await SearchBinanceAsync(query, ct)
            : await SearchYahooAsync(query, ct);
    }

    public async Task<bool> IsValidSymbolAsync(MarketType marketType, string symbol, CancellationToken ct)
    {
        symbol = NormalizeSymbol(symbol);
        if (string.IsNullOrWhiteSpace(symbol))
            return false;

        if (marketType == MarketType.Crypto)
        {
            var symbols = await GetBinanceSymbolsAsync(ct);
            return symbols.Any(x => string.Equals(x.Symbol, symbol, StringComparison.OrdinalIgnoreCase));
        }

        return await HasYahooChartAsync(symbol, ct);
    }

    private async Task<IReadOnlyList<SymbolSearchResult>> SearchBinanceAsync(string query, CancellationToken ct)
    {
        var normalized = NormalizeSymbol(query);
        var symbols = await GetBinanceSymbolsAsync(ct);

        return symbols
            .Select(x => new
            {
                Symbol = x,
                Rank = GetSearchRank(x, normalized)
            })
            .Where(x => x.Rank < int.MaxValue)
            .OrderBy(x => x.Rank)
            .ThenBy(x => GetCryptoPairRank(x.Symbol, normalized))
            .ThenBy(x => x.Symbol.Symbol)
            .Take(30)
            .Select(x => x.Symbol)
            .ToList();
    }

    private async Task<IReadOnlyList<SymbolSearchResult>> GetBinanceSymbolsAsync(CancellationToken ct)
    {
        if (_binanceSymbols is not null && DateTime.UtcNow - _binanceLoadedAtUtc < TimeSpan.FromHours(6))
            return _binanceSymbols;

        await _binanceLock.WaitAsync(ct);
        try
        {
            if (_binanceSymbols is not null && DateTime.UtcNow - _binanceLoadedAtUtc < TimeSpan.FromHours(6))
                return _binanceSymbols;

            var baseUrl = _options.CurrentValue.BinanceBaseUrl.TrimEnd('/');
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.CurrentValue.RequestTimeoutSeconds, 10, 60)));

            using var response = await _httpClientFactory.CreateClient("market-data").GetAsync($"{baseUrl}/api/v3/exchangeInfo", timeout.Token);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);

            var symbols = new List<SymbolSearchResult>();
            foreach (var item in doc.RootElement.GetProperty("symbols").EnumerateArray())
            {
                var status = item.GetProperty("status").GetString();
                if (!string.Equals(status, "TRADING", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (item.TryGetProperty("isSpotTradingAllowed", out var spotAllowed) &&
                    spotAllowed.ValueKind is JsonValueKind.False)
                    continue;

                var symbol = item.GetProperty("symbol").GetString();
                var baseAsset = item.GetProperty("baseAsset").GetString();
                var quoteAsset = item.GetProperty("quoteAsset").GetString();
                if (string.IsNullOrWhiteSpace(symbol))
                    continue;

                symbols.Add(new SymbolSearchResult(
                    symbol,
                    $"{baseAsset}/{quoteAsset}",
                    MarketType.Crypto.ToString(),
                    "Binance"));
            }

            _binanceSymbols = symbols.OrderBy(x => x.Symbol).ToList();
            _binanceLoadedAtUtc = DateTime.UtcNow;
            return _binanceSymbols;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load Binance symbol catalog.");
            return _binanceSymbols ?? Array.Empty<SymbolSearchResult>();
        }
        finally
        {
            _binanceLock.Release();
        }
    }

    private async Task<IReadOnlyList<SymbolSearchResult>> SearchYahooAsync(string query, CancellationToken ct)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.CurrentValue.RequestTimeoutSeconds, 1, 30)));

            var uri = $"https://query1.finance.yahoo.com/v1/finance/search?q={Uri.EscapeDataString(query)}&quotesCount=20&newsCount=0";
            using var response = await _httpClientFactory.CreateClient("market-data").GetAsync(uri, timeout.Token);
            if (!response.IsSuccessStatusCode)
                return Array.Empty<SymbolSearchResult>();

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);
            if (!doc.RootElement.TryGetProperty("quotes", out var quotes) || quotes.ValueKind != JsonValueKind.Array)
                return Array.Empty<SymbolSearchResult>();

            var results = new List<SymbolSearchResult>();
            foreach (var quote in quotes.EnumerateArray())
            {
                if (!quote.TryGetProperty("symbol", out var symbolElement))
                    continue;

                var symbol = symbolElement.GetString();
                if (string.IsNullOrWhiteSpace(symbol))
                    continue;

                var name = GetString(quote, "shortname") ?? GetString(quote, "longname") ?? symbol;
                var exchange = GetString(quote, "exchDisp") ?? GetString(quote, "exchange");
                results.Add(new SymbolSearchResult(
                    symbol.ToUpperInvariant(),
                    name,
                    MarketType.Traditional.ToString(),
                    exchange));
            }

            return results
                .GroupBy(x => x.Symbol, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .Take(20)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not search Yahoo symbols for query {Query}.", query);
            return Array.Empty<SymbolSearchResult>();
        }
    }

    private async Task<bool> HasYahooChartAsync(string symbol, CancellationToken ct)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.CurrentValue.RequestTimeoutSeconds, 1, 30)));

            var uri = $"https://query2.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(symbol)}?range=5d&interval=1d";
            using var response = await _httpClientFactory.CreateClient("market-data").GetAsync(uri, timeout.Token);
            if (!response.IsSuccessStatusCode)
                return false;

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);
            return doc.RootElement.TryGetProperty("chart", out var chart) &&
                chart.TryGetProperty("result", out var results) &&
                results.ValueKind == JsonValueKind.Array &&
                results.GetArrayLength() > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not validate Yahoo symbol {Symbol}.", symbol);
            return false;
        }
    }

    private static string NormalizeSymbol(string? symbol)
    {
        return (symbol ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static int GetSearchRank(SymbolSearchResult result, string query)
    {
        if (result.Symbol.Equals(query, StringComparison.OrdinalIgnoreCase))
            return 0;

        if (result.Symbol.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            return 1;

        if (result.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            return 2;

        if (result.Symbol.Contains(query, StringComparison.OrdinalIgnoreCase))
            return 3;

        if (result.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            return 4;

        return int.MaxValue;
    }

    private static int GetCryptoPairRank(SymbolSearchResult result, string query)
    {
        var symbol = result.Symbol;
        if (symbol.Equals($"{query}USDT", StringComparison.OrdinalIgnoreCase))
            return 0;

        if (symbol.EndsWith("USDT", StringComparison.OrdinalIgnoreCase))
            return 1;

        if (symbol.EndsWith("USDC", StringComparison.OrdinalIgnoreCase))
            return 2;

        if (symbol.EndsWith("BTC", StringComparison.OrdinalIgnoreCase))
            return 3;

        return 4;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
