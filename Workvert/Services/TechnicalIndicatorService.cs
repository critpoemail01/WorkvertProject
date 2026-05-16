using System.Globalization;
using System.Text.Json;
using Workvert.Models;
using Microsoft.Extensions.Options;

namespace Workvert.Services;

public sealed class TechnicalIndicatorService : ITechnicalIndicatorService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<MarketDataOptions> _options;
    private readonly ILogger<TechnicalIndicatorService> _logger;

    public TechnicalIndicatorService(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<MarketDataOptions> options,
        ILogger<TechnicalIndicatorService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<TechnicalIndicatorSnapshot?> GetSnapshotAsync(
        string symbol,
        MarketType marketType,
        string timeframe,
        int rsiPeriod,
        int fastEmaPeriod,
        int slowEmaPeriod,
        CancellationToken ct)
    {
        symbol = (symbol ?? string.Empty).Trim().ToUpperInvariant();
        timeframe = NormalizeTimeframe(timeframe);
        rsiPeriod = Math.Clamp(rsiPeriod, 2, 100);
        fastEmaPeriod = Math.Clamp(fastEmaPeriod, 1, 200);
        slowEmaPeriod = Math.Clamp(slowEmaPeriod, fastEmaPeriod + 1, 250);

        IReadOnlyList<decimal> closes;
        if (string.Equals(_options.CurrentValue.Provider, "Fake", StringComparison.OrdinalIgnoreCase))
        {
            closes = CreateSyntheticCloses(symbol, 160);
        }
        else
        {
            closes = await FetchClosesAsync(symbol, marketType, timeframe, ct);
            if (closes.Count == 0 && _options.CurrentValue.FallbackToFake)
                closes = CreateSyntheticCloses(symbol, 160);
        }

        var minPoints = new[] { rsiPeriod + 1, fastEmaPeriod + 2, slowEmaPeriod + 2 }.Max();
        if (closes.Count < minPoints)
        {
            _logger.LogWarning("Not enough candles to calculate indicators for {Symbol} {Timeframe}. Got {Count}, need {MinPoints}.", symbol, timeframe, closes.Count, minPoints);
            return null;
        }

        var rsi = CalculateRsi(closes, rsiPeriod);
        var fastEma = CalculateEma(closes, fastEmaPeriod);
        var slowEma = CalculateEma(closes, slowEmaPeriod);
        if (rsi is null || fastEma.Count < 2 || slowEma.Count < 2)
            return null;

        return new TechnicalIndicatorSnapshot(
            symbol,
            timeframe,
            decimal.Round(rsi.Value, 4),
            decimal.Round(fastEma[^1], 8),
            decimal.Round(slowEma[^1], 8),
            decimal.Round(fastEma[^2], 8),
            decimal.Round(slowEma[^2], 8),
            DateTime.UtcNow);
    }

    private async Task<IReadOnlyList<decimal>> FetchClosesAsync(string symbol, MarketType marketType, string timeframe, CancellationToken ct)
    {
        if (marketType == MarketType.Crypto)
        {
            var binance = await FetchBinanceClosesAsync(symbol, timeframe, ct);
            if (binance.Count > 0)
                return binance;
        }

        return await FetchYahooClosesAsync(symbol, timeframe, ct);
    }

    private async Task<IReadOnlyList<decimal>> FetchBinanceClosesAsync(string symbol, string timeframe, CancellationToken ct)
    {
        try
        {
            var normalized = new string(symbol.Where(char.IsLetterOrDigit).ToArray());
            var interval = ToBinanceInterval(timeframe);
            var baseUrl = _options.CurrentValue.BinanceBaseUrl.TrimEnd('/');
            var uri = $"{baseUrl}/api/v3/klines?symbol={Uri.EscapeDataString(normalized)}&interval={interval}&limit=160";

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.CurrentValue.RequestTimeoutSeconds, 1, 30)));

            using var response = await _httpClientFactory.CreateClient("market-data").GetAsync(uri, timeout.Token);
            if (!response.IsSuccessStatusCode)
                return Array.Empty<decimal>();

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);
            var closes = new List<decimal>();

            foreach (var row in doc.RootElement.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Array || row.GetArrayLength() < 5)
                    continue;

                var closeText = row[4].GetString();
                if (TryDecimal(closeText, out var close))
                    closes.Add(close);
            }

            return closes;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch Binance candles for {Symbol} {Timeframe}.", symbol, timeframe);
            return Array.Empty<decimal>();
        }
    }

    private async Task<IReadOnlyList<decimal>> FetchYahooClosesAsync(string symbol, string timeframe, CancellationToken ct)
    {
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var from = DateTimeOffset.UtcNow.AddDays(-DaysBack(timeframe)).ToUnixTimeSeconds();
            var uri = $"https://query2.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(symbol)}?period1={from}&period2={now}&interval={ToYahooInterval(timeframe)}&events=div,splits";

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.CurrentValue.RequestTimeoutSeconds, 1, 30)));

            using var response = await _httpClientFactory.CreateClient("market-data").GetAsync(uri, timeout.Token);
            if (!response.IsSuccessStatusCode)
                return Array.Empty<decimal>();

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);

            if (!doc.RootElement.TryGetProperty("chart", out var chart) ||
                !chart.TryGetProperty("result", out var results) ||
                results.ValueKind != JsonValueKind.Array ||
                results.GetArrayLength() == 0)
            {
                return Array.Empty<decimal>();
            }

            var result = results[0];
            var quote = result.GetProperty("indicators").GetProperty("quote")[0];
            var closes = new List<decimal>();

            foreach (var closeElement in quote.GetProperty("close").EnumerateArray())
            {
                if (closeElement.ValueKind != JsonValueKind.Number)
                    continue;

                closes.Add(closeElement.GetDecimal());
            }

            return closes;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch Yahoo candles for {Symbol} {Timeframe}.", symbol, timeframe);
            return Array.Empty<decimal>();
        }
    }

    private static decimal? CalculateRsi(IReadOnlyList<decimal> closes, int period)
    {
        if (closes.Count <= period)
            return null;

        decimal gain = 0;
        decimal loss = 0;

        for (var i = 1; i <= period; i++)
        {
            var change = closes[i] - closes[i - 1];
            if (change >= 0) gain += change;
            else loss -= change;
        }

        var avgGain = gain / period;
        var avgLoss = loss / period;

        for (var i = period + 1; i < closes.Count; i++)
        {
            var change = closes[i] - closes[i - 1];
            var currentGain = change > 0 ? change : 0;
            var currentLoss = change < 0 ? -change : 0;
            avgGain = ((avgGain * (period - 1)) + currentGain) / period;
            avgLoss = ((avgLoss * (period - 1)) + currentLoss) / period;
        }

        if (avgLoss == 0)
            return 100;
        if (avgGain == 0)
            return 0;

        var rs = avgGain / avgLoss;
        return 100 - (100 / (1 + rs));
    }

    private static List<decimal> CalculateEma(IReadOnlyList<decimal> closes, int period)
    {
        var ema = new List<decimal>(closes.Count);
        if (closes.Count == 0)
            return ema;

        var multiplier = 2m / (period + 1);
        var current = closes[0];
        ema.Add(current);

        for (var i = 1; i < closes.Count; i++)
        {
            current = (closes[i] - current) * multiplier + current;
            ema.Add(current);
        }

        return ema;
    }

    private static IReadOnlyList<decimal> CreateSyntheticCloses(string symbol, int count)
    {
        var seed = symbol.Aggregate(17, (acc, ch) => (acc * 31) + ch);
        var random = new Random(seed);
        var price = 50m + Math.Abs(seed % 500);
        var closes = new List<decimal>(count);

        for (var i = 0; i < count; i++)
        {
            var drift = (decimal)((random.NextDouble() - 0.48) * 2.0);
            price = Math.Max(0.01m, price + drift);
            closes.Add(decimal.Round(price, 4));
        }

        return closes;
    }

    private static string NormalizeTimeframe(string timeframe)
    {
        return timeframe?.Trim().ToLowerInvariant() switch
        {
            "5m" => "5m",
            "15m" => "15m",
            "1h" => "1h",
            "4h" => "4h",
            "1d" => "1d",
            "1wk" or "1w" => "1wk",
            "1mo" or "1mth" => "1mo",
            _ => "4h"
        };
    }

    private static string ToBinanceInterval(string timeframe)
    {
        return timeframe switch
        {
            "1wk" => "1w",
            "1mo" => "1M",
            _ => timeframe
        };
    }

    private static string ToYahooInterval(string timeframe)
    {
        return timeframe switch
        {
            "1wk" => "1wk",
            "1mo" => "1mo",
            _ => timeframe
        };
    }

    private static int DaysBack(string timeframe)
    {
        return timeframe switch
        {
            "5m" or "15m" => 10,
            "1h" or "4h" => 60,
            "1d" => 420,
            "1wk" => 5 * 365,
            "1mo" => 10 * 365,
            _ => 60
        };
    }

    private static bool TryDecimal(string? value, out decimal result)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }
}
