using System.Globalization;
using System.Net;
using System.Text.Json;
using Alivert.Models;
using Microsoft.Extensions.Options;

namespace Alivert.Services;

public sealed class MarketDataService : IMarketDataService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FakeMarketDataService _fallback;
    private readonly IOptionsMonitor<MarketDataOptions> _options;
    private readonly ILogger<MarketDataService> _logger;

    public MarketDataService(
        IHttpClientFactory httpClientFactory,
        FakeMarketDataService fallback,
        IOptionsMonitor<MarketDataOptions> options,
        ILogger<MarketDataService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _fallback = fallback;
        _options = options;
        _logger = logger;
    }

    public async Task<MarketSnapshot> GetSnapshotAsync(string symbol, MarketType marketType, CancellationToken ct)
    {
        var options = _options.CurrentValue;

        if (marketType == MarketType.Traditional &&
            !string.Equals(options.Provider, "Fake", StringComparison.OrdinalIgnoreCase))
        {
            return await GetYahooSnapshotAsync(symbol, ct);
        }

        if (!string.Equals(options.Provider, "Binance", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(options.Provider, "Fake", StringComparison.OrdinalIgnoreCase))
                return await _fallback.GetSnapshotAsync(symbol, marketType, ct);
        }

        var normalized = NormalizeSymbol(symbol);

        try
        {
            var baseUrl = options.BinanceBaseUrl.TrimEnd('/');
            var requestUri = $"{baseUrl}/api/v3/ticker/24hr?symbol={Uri.EscapeDataString(normalized)}";
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(options.RequestTimeoutSeconds, 1, 30)));

            using var response = await _httpClientFactory
                .CreateClient("market-data")
                .GetAsync(requestUri, timeout.Token);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
                    throw new InvalidOperationException($"Binance symbol {normalized} was not found.");

                _logger.LogWarning("Binance market data request failed for {Symbol} with status {StatusCode}.", normalized, (int)response.StatusCode);
                return await FallbackOrThrowAsync(symbol, marketType, ct);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            var ticker = await JsonSerializer.DeserializeAsync<BinanceTickerResponse>(stream, JsonOptions, timeout.Token);
            if (ticker is null ||
                !TryDecimal(ticker.LastPrice, out var price) ||
                !TryDecimal(ticker.PriceChangePercent, out var percentChange24h))
            {
                _logger.LogWarning("Binance market data response for {Symbol} was missing required price fields.", normalized);
                return await FallbackOrThrowAsync(symbol, marketType, ct);
            }

            TryDecimal(ticker.QuoteVolume, out var volume24h);
            return new MarketSnapshot(
                ticker.Symbol ?? normalized,
                decimal.Round(price, 8),
                decimal.Round(percentChange24h, 4),
                DateTime.UtcNow,
                volume24h > 0 ? decimal.Round(volume24h, 2) : null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Binance market data request timed out for {Symbol}.", normalized);
            return await FallbackOrThrowAsync(symbol, marketType, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("was not found", StringComparison.Ordinal))
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Binance market data request failed for {Symbol}.", normalized);
            return await FallbackOrThrowAsync(symbol, marketType, ct);
        }
    }

    private async Task<MarketSnapshot> GetYahooSnapshotAsync(string symbol, CancellationToken ct)
    {
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var from = DateTimeOffset.UtcNow.AddDays(-10).ToUnixTimeSeconds();
            var uri = $"https://query2.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(symbol)}?period1={from}&period2={now}&interval=1d&events=div,splits";

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.CurrentValue.RequestTimeoutSeconds, 1, 30)));

            using var response = await _httpClientFactory.CreateClient("market-data").GetAsync(uri, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
                    throw new InvalidOperationException($"Yahoo Finance symbol {symbol} was not found.");

                return await FallbackOrThrowAsync(symbol, MarketType.Traditional, ct);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);
            if (!doc.RootElement.TryGetProperty("chart", out var chart) ||
                !chart.TryGetProperty("result", out var results) ||
                results.ValueKind != JsonValueKind.Array ||
                results.GetArrayLength() == 0)
            {
                return await FallbackOrThrowAsync(symbol, MarketType.Traditional, ct);
            }

            var quote = results[0].GetProperty("indicators").GetProperty("quote")[0];
            var closes = quote.GetProperty("close")
                .EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.Number)
                .Select(x => x.GetDecimal())
                .ToList();

            if (closes.Count == 0)
                return await FallbackOrThrowAsync(symbol, MarketType.Traditional, ct);

            var price = closes[^1];
            var previous = closes.Count > 1 ? closes[^2] : price;
            var percentChange = previous == 0 ? 0 : ((price - previous) / previous) * 100m;

            decimal? volume = null;
            if (quote.TryGetProperty("volume", out var volumes))
            {
                var latestVolume = volumes.EnumerateArray().LastOrDefault(x => x.ValueKind == JsonValueKind.Number);
                if (latestVolume.ValueKind == JsonValueKind.Number)
                    volume = latestVolume.GetDecimal();
            }

            return new MarketSnapshot(
                symbol.ToUpperInvariant(),
                decimal.Round(price, 4),
                decimal.Round(percentChange, 4),
                DateTime.UtcNow,
                volume);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("was not found", StringComparison.Ordinal))
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Yahoo market data request failed for {Symbol}.", symbol);
            return await FallbackOrThrowAsync(symbol, MarketType.Traditional, ct);
        }
    }

    private Task<MarketSnapshot> FallbackOrThrowAsync(string symbol, MarketType marketType, CancellationToken ct)
    {
        if (_options.CurrentValue.FallbackToFake)
            return _fallback.GetSnapshotAsync(symbol, marketType, ct);

        throw new InvalidOperationException($"Could not fetch market data for {symbol}.");
    }

    private static string NormalizeSymbol(string symbol)
    {
        return new string((symbol ?? string.Empty)
            .Trim()
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private static bool TryDecimal(string? value, out decimal result)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }

    private sealed class BinanceTickerResponse
    {
        public string? Symbol { get; set; }
        public string? LastPrice { get; set; }
        public string? PriceChangePercent { get; set; }
        public string? QuoteVolume { get; set; }
    }
}
