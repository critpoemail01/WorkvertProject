using Dealvert.Models;
using Microsoft.Extensions.Options;

namespace Dealvert.Services;

public sealed class MarketDataService : IMarketDataService
{
    private readonly FakeMarketDataService _fallback;
    private readonly IOptionsMonitor<MarketDataOptions> _options;
    private readonly ILogger<MarketDataService> _logger;

    public MarketDataService(
        IHttpClientFactory httpClientFactory,
        FakeMarketDataService fallback,
        IOptionsMonitor<MarketDataOptions> options,
        ILogger<MarketDataService> logger)
    {
        _fallback = fallback;
        _options = options;
        _logger = logger;
    }

    public Task<MarketSnapshot> GetSnapshotAsync(string symbol, MarketType marketType, CancellationToken ct, Alert? alert = null)
    {
        ct.ThrowIfCancellationRequested();

        if (!LooksLikeProductUrl(symbol))
        {
            _logger.LogDebug("Using fallback market snapshot for non-product source {Source}.", symbol);
            return _fallback.GetSnapshotAsync(symbol, marketType, ct, alert);
        }

        var metadata = ProductWatchMetadata.Parse(alert?.AudienceList);
        var uri = new Uri(symbol.Trim());
        var sourceStore = NormalizeStoreName(uri.Host);
        var trustedStores = metadata.TrustedStoreList();
        var secondHandSites = metadata.SecondHandSiteList();
        var store = PickStore(symbol, trustedStores.Count > 0 ? trustedStores : new[] { sourceStore });
        var productName = ProductNameFromUrl(uri);

        var seed = StableSeed($"{symbol}|{metadata.Country}|{metadata.City}|{metadata.Category}|{store}");
        var basePrice = 24m + (seed % 160000) / 100m;
        var storeAdjustment = ((seed / 97) % 2300 - 1050) / 100m;
        var localAdjustment = metadata.Country.Equals("Portugal", StringComparison.OrdinalIgnoreCase) ? 1.5m : 0m;
        var currentPrice = Math.Max(1m, basePrice * (1 + (storeAdjustment + localAdjustment) / 100m));
        var dailyMove = ((seed / 193) % 1800 - 900) / 100m;
        var score = Math.Clamp(Math.Abs(dailyMove) * 5m + (storeAdjustment < 0 ? Math.Abs(storeAdjustment) * 2m : 0m), 4m, 98m);

        var secondHandPremium = 7m + ((seed / 389) % 3600) / 100m;
        var resaleBenchmark = currentPrice * (1 + secondHandPremium / 100m);
        var margin = resaleBenchmark == 0 ? 0 : (resaleBenchmark - currentPrice) / resaleBenchmark * 100m;

        var destination = metadata.LocationLabel();
        var bestSecondHandSite = PickStore(symbol, secondHandSites.Count > 0 ? secondHandSites : new[] { "OLX", "Vinted", "Wallapop" });
        var symbolLabel = $"{productName} @ {store}";

        return Task.FromResult(new MarketSnapshot(
            symbolLabel,
            decimal.Round(currentPrice, 2),
            decimal.Round(dailyMove, 2),
            DateTime.UtcNow,
            decimal.Round(score, 2),
            store,
            productName,
            symbol,
            decimal.Round(resaleBenchmark, 2),
            decimal.Round(margin, 2),
            metadata.Country,
            string.IsNullOrWhiteSpace(metadata.City) ? destination : metadata.City,
            $"{metadata.Category} via {bestSecondHandSite}"));
    }

    private static bool LooksLikeProductUrl(string source)
    {
        return Uri.TryCreate(source?.Trim(), UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static string NormalizeStoreName(string host)
    {
        var clean = host.ToLowerInvariant();
        if (clean.StartsWith("www.", StringComparison.Ordinal))
            clean = clean[4..];

        return clean switch
        {
            var h when h.Contains("amazon.") => "Amazon",
            var h when h.Contains("worten.") => "Worten",
            var h when h.Contains("fnac.") => "Fnac",
            var h when h.Contains("pccomponentes.") => "PcComponentes",
            var h when h.Contains("mediamarkt.") => "MediaMarkt",
            _ => clean
        };
    }

    private static string ProductNameFromUrl(Uri uri)
    {
        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(segment => !segment.Equals("dp", StringComparison.OrdinalIgnoreCase))
            .Where(segment => !segment.Equals("gp", StringComparison.OrdinalIgnoreCase))
            .Where(segment => segment.Length > 2)
            .ToArray();

        var slug = segments.FirstOrDefault(segment => segment.Any(char.IsLetter)) ?? uri.Host;
        slug = Uri.UnescapeDataString(slug)
            .Replace("-", " ")
            .Replace("_", " ")
            .Trim();

        return string.IsNullOrWhiteSpace(slug) ? uri.Host : CapitalizeWords(slug);
    }

    private static string CapitalizeWords(string value)
    {
        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(' ', words.Select(word =>
            word.Length <= 1
                ? word.ToUpperInvariant()
                : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
    }

    private static string PickStore(string key, IReadOnlyList<string> stores)
    {
        if (stores.Count == 0)
            return "Verified store";

        var index = Math.Abs(StableSeed(key)) % stores.Count;
        return stores[index];
    }

    private static int StableSeed(string value)
    {
        unchecked
        {
            var hash = 23;
            foreach (var ch in value)
                hash = (hash * 31) + ch;

            return hash == int.MinValue ? 0 : Math.Abs(hash);
        }
    }
}
