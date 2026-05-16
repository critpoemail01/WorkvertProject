using System.Globalization;
using System.Text;

namespace Dealvert.Models;

public sealed record ProductWatchMetadata(
    string Country,
    string City,
    string Category,
    string TrustedStores,
    string SecondHandSites,
    string ReportEmail,
    string LocationScope = "City",
    decimal? Latitude = null,
    decimal? Longitude = null,
    int? RadiusKm = null)
{
    public static ProductWatchMetadata Default => new(
        "Portugal",
        "Lisbon",
        "Electronics",
        "Amazon.es, Worten, Fnac, PcComponentes, MediaMarkt",
        "OLX, Vinted, Wallapop",
        string.Empty,
        "City",
        38.7223m,
        -9.1393m,
        25);

    public static ProductWatchMetadata Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Default;

        var map = value
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);

        return new ProductWatchMetadata(
            Get(map, nameof(Country), Default.Country),
            Get(map, nameof(City), Default.City),
            Get(map, nameof(Category), Default.Category),
            Get(map, nameof(TrustedStores), Default.TrustedStores),
            Get(map, nameof(SecondHandSites), Default.SecondHandSites),
            Get(map, nameof(ReportEmail), Default.ReportEmail),
            Get(map, nameof(LocationScope), InferScope(map)),
            GetDecimal(map, nameof(Latitude)),
            GetDecimal(map, nameof(Longitude)),
            GetInt(map, nameof(RadiusKm)));
    }

    public string Serialize()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"{nameof(LocationScope)}={Clean(LocationScope)}");
        builder.AppendLine($"{nameof(Country)}={Clean(Country)}");
        builder.AppendLine($"{nameof(City)}={Clean(City)}");
        if (Latitude is not null)
            builder.AppendLine($"{nameof(Latitude)}={Latitude.Value.ToString("0.######", CultureInfo.InvariantCulture)}");
        if (Longitude is not null)
            builder.AppendLine($"{nameof(Longitude)}={Longitude.Value.ToString("0.######", CultureInfo.InvariantCulture)}");
        if (RadiusKm is not null)
            builder.AppendLine($"{nameof(RadiusKm)}={RadiusKm.Value.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"{nameof(Category)}={Clean(Category)}");
        builder.AppendLine($"{nameof(TrustedStores)}={Clean(TrustedStores)}");
        builder.AppendLine($"{nameof(SecondHandSites)}={Clean(SecondHandSites)}");
        builder.AppendLine($"{nameof(ReportEmail)}={Clean(ReportEmail)}");
        return builder.ToString().Trim();
    }

    public string LocationLabel()
    {
        var scope = Clean(LocationScope);
        var city = Clean(City);
        var country = Clean(Country);
        if (scope.Equals("World", StringComparison.OrdinalIgnoreCase))
            return "Worldwide";

        if (scope.Equals("Country", StringComparison.OrdinalIgnoreCase))
            return string.IsNullOrWhiteSpace(country) ? "Selected country" : country;

        if (scope.Equals("Custom", StringComparison.OrdinalIgnoreCase))
        {
            var place = string.Join(", ", new[] { city, country }.Where(x => !string.IsNullOrWhiteSpace(x)));
            place = string.IsNullOrWhiteSpace(place) ? "Custom area" : place;
            return RadiusKm is > 0 ? $"{place} within {RadiusKm} km" : place;
        }

        if (string.IsNullOrWhiteSpace(city))
            return string.IsNullOrWhiteSpace(country) ? "Worldwide" : country;

        return string.IsNullOrWhiteSpace(country) ? city : $"{city}, {country}";
    }

    public IReadOnlyList<string> TrustedStoreList() => SplitList(TrustedStores);

    public IReadOnlyList<string> SecondHandSiteList() => SplitList(SecondHandSites);

    private static string Get(IReadOnlyDictionary<string, string> map, string key, string fallback)
    {
        return map.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : fallback;
    }

    private static string InferScope(IReadOnlyDictionary<string, string> map)
    {
        if (map.TryGetValue(nameof(RadiusKm), out var radius) && !string.IsNullOrWhiteSpace(radius))
            return "Custom";

        if (map.TryGetValue(nameof(City), out var city) && !string.IsNullOrWhiteSpace(city))
            return "City";

        if (map.TryGetValue(nameof(Country), out var country) && !string.IsNullOrWhiteSpace(country) && !country.Equals("World", StringComparison.OrdinalIgnoreCase))
            return "Country";

        return "World";
    }

    private static decimal? GetDecimal(IReadOnlyDictionary<string, string> map, string key)
    {
        return map.TryGetValue(key, out var value) &&
            decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static int? GetInt(IReadOnlyDictionary<string, string> map, string key)
    {
        return map.TryGetValue(key, out var value) &&
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static string Clean(string? value) => (value ?? string.Empty).Trim();

    private static IReadOnlyList<string> SplitList(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }
}
