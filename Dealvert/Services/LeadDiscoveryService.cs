using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Alivert.Services;

public sealed class LeadDiscoveryService : ILeadDiscoveryService
{
    private const int MaxUrlsPerRequest = 8;
    private const int MaxOnlineSearchQueries = 4;
    private const int MaxOnlineWebsites = 8;
    private const int MaxPagesPerWebsite = 5;
    private const int MaxPageChars = 220_000;
    private static readonly Regex EmailRegex = new(
        @"[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,24}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex HrefRegex = new(
        "href\\s*=\\s*[\"'](?<url>https?://[^\"'<>]+)[\"']",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly HttpClient _httpClient;

    public LeadDiscoveryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LeadDiscoveryResult> DiscoverAsync(LeadDiscoveryRequest request, CancellationToken cancellationToken = default)
    {
        var queries = BuildSearchQueries(request);
        var warnings = new List<string>();
        var discovered = new Dictionary<string, DiscoveredLeadEmail>(StringComparer.OrdinalIgnoreCase);
        var websites = ParseCompanyUrls(request.CompanyWebsiteUrls).Take(MaxUrlsPerRequest).ToList();

        if (request.SearchOnline)
        {
            var onlineWebsites = await FindOnlineCandidateWebsitesAsync(queries, cancellationToken);
            websites = websites
                .Concat(onlineWebsites)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxUrlsPerRequest + MaxOnlineWebsites)
                .ToList();

            if (onlineWebsites.Count == 0)
                warnings.Add("No company websites were found automatically for this audience and location. Review the prospecting searches or paste known company websites.");
        }

        foreach (var website in websites)
        {
            if (!TryCreatePublicUri(website, out var rootUri, out var rejectReason))
            {
                warnings.Add(rejectReason);
                continue;
            }

            foreach (var pageUri in CandidatePages(rootUri).Take(MaxPagesPerWebsite))
            {
                string html;
                try
                {
                    html = await DownloadPageAsync(pageUri, cancellationToken);
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
                {
                    continue;
                }

                foreach (var email in ExtractEmails(html))
                {
                    discovered.TryAdd(email, new DiscoveredLeadEmail(email, pageUri.ToString(), rootUri.Host));
                }
            }
        }

        if (websites.Count > 0 && discovered.Count == 0)
        {
            warnings.Add(request.SearchOnline
                ? "No public email addresses were found automatically on the candidate company websites. You can still paste emails manually."
                : "No public email addresses were found on the supplied company websites. Try adding the company contact page URLs directly.");
        }

        return new LeadDiscoveryResult(
            queries,
            discovered.Values.OrderBy(x => x.SourceHost).ThenBy(x => x.Email).ToList(),
            warnings);
    }

    private static IReadOnlyList<LeadSearchQuery> BuildSearchQueries(LeadDiscoveryRequest request)
    {
        var audience = CleanQueryPart(request.TargetAudience, "potential customers");
        var goal = CleanQueryPart(request.CampaignGoal, "subscriptions");
        var location = CleanQueryPart(request.LocationSummary, "worldwide");
        var product = CleanQueryPart(request.ProductContext, audience);
        var segments = SegmentKeywords(request.TargetAudience).Take(4).ToList();
        var queries = new List<LeadSearchQuery>
        {
            Search("Companies likely to need this product", $"{product} {audience} companies {location} contact email"),
            Search("Companies matching target audience", $"{audience} companies {location} contact"),
            Search("Decision makers to contact", $"{audience} founder marketing manager sales lead {location}"),
            Search("Directories and associations", $"{audience} business directory association {location}"),
            Search("Potential buyers with public contact page", $"{audience} {goal} contact email {location}")
        };

        foreach (var segment in segments)
        {
            queries.Add(Search($"{segment} prospects", $"{segment} companies contact {location}"));
        }

        return queries;
    }

    private static LeadSearchQuery Search(string label, string query)
    {
        return new LeadSearchQuery(label, $"https://www.bing.com/search?q={Uri.EscapeDataString(query)}");
    }

    private async Task<IReadOnlyList<string>> FindOnlineCandidateWebsitesAsync(
        IReadOnlyList<LeadSearchQuery> queries,
        CancellationToken cancellationToken)
    {
        var websites = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var query in queries.Take(MaxOnlineSearchQueries))
        {
            string html;
            try
            {
                html = await DownloadPageAsync(new Uri(query.Url), cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                continue;
            }

            foreach (var resultUrl in ExtractSearchResultUrls(html))
            {
                if (!TryCreatePublicUri(resultUrl, out var candidateRoot, out _) ||
                    IsSearchOrSocialHost(candidateRoot.Host))
                {
                    continue;
                }

                var candidate = candidateRoot.ToString();
                if (!seen.Add(candidate))
                    continue;

                websites.Add(candidate);
                if (websites.Count >= MaxOnlineWebsites)
                    return websites;
            }
        }

        return websites;
    }

    private static IEnumerable<string> ExtractSearchResultUrls(string html)
    {
        foreach (Match match in HrefRegex.Matches(html))
        {
            var decoded = WebUtility.HtmlDecode(match.Groups["url"].Value);
            var normalized = DecodeBingRedirect(decoded);
            if (!string.IsNullOrWhiteSpace(normalized))
                yield return normalized;
        }
    }

    private static string DecodeBingRedirect(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !uri.Host.EndsWith("bing.com", StringComparison.OrdinalIgnoreCase) ||
            !uri.AbsolutePath.StartsWith("/ck/", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        var encodedTarget = QueryValue(uri, "u");
        if (string.IsNullOrWhiteSpace(encodedTarget))
            return url;

        if (encodedTarget.StartsWith("a1", StringComparison.OrdinalIgnoreCase))
            encodedTarget = encodedTarget[2..];

        try
        {
            var padded = encodedTarget.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            return Uri.TryCreate(decoded, UriKind.Absolute, out _) ? decoded : url;
        }
        catch (FormatException)
        {
            return url;
        }
    }

    private static string? QueryValue(Uri uri, string name)
    {
        foreach (var part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pieces = part.Split('=', 2);
            if (pieces.Length == 2 && pieces[0].Equals(name, StringComparison.OrdinalIgnoreCase))
                return WebUtility.UrlDecode(pieces[1]);
        }

        return null;
    }

    private static bool IsSearchOrSocialHost(string host)
    {
        var normalized = host.ToLowerInvariant();
        return normalized.Contains("bing.com") ||
            normalized.Contains("google.") ||
            normalized.Contains("duckduckgo.") ||
            normalized.Contains("microsoft.com") ||
            normalized.Contains("linkedin.com") ||
            normalized.Contains("facebook.com") ||
            normalized.Contains("instagram.com") ||
            normalized.Contains("youtube.com") ||
            normalized.Contains("x.com") ||
            normalized.Contains("twitter.com");
    }

    private static IEnumerable<string> SegmentKeywords(string targetAudience)
    {
        var audience = targetAudience.ToLowerInvariant();
        if (audience.Contains("industrial") || audience.Contains("fabr") || audience.Contains("manufactur") || audience.Contains("produc"))
        {
            yield return "industrial SMEs";
            yield return "manufacturing companies";
            yield return "maintenance companies";
            yield return "metalworking companies";
            yield break;
        }

        if (audience.Contains("saas") || audience.Contains("b2b") || audience.Contains("software"))
        {
            yield return "B2B SaaS companies";
            yield return "software agencies";
            yield return "IT services companies";
            yield return "operations teams";
            yield break;
        }

        if (audience.Contains("restaurant") || audience.Contains("food") || audience.Contains("hotel"))
        {
            yield return "restaurants";
            yield return "hospitality groups";
            yield return "local franchises";
            yield return "event venues";
            yield break;
        }

        yield return "small businesses";
        yield return "professional services";
        yield return "online businesses";
        yield return "digital commerce companies";
    }

    private static IEnumerable<string> ParseCompanyUrls(string? urls)
    {
        if (string.IsNullOrWhiteSpace(urls))
            yield break;

        foreach (var item in urls.Split(new[] { '\r', '\n', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrWhiteSpace(item))
                yield return item;
        }
    }

    private static bool TryCreatePublicUri(string value, out Uri uri, out string rejectReason)
    {
        uri = null!;
        rejectReason = string.Empty;
        var normalized = value.Contains("://", StringComparison.Ordinal) ? value : $"https://{value}";
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var parsed) ||
            parsed.Scheme is not ("http" or "https") ||
            string.IsNullOrWhiteSpace(parsed.Host))
        {
            rejectReason = $"Skipped '{value}' because it is not a valid company URL.";
            return false;
        }

        if (IsBlockedHost(parsed.Host))
        {
            rejectReason = $"Skipped '{value}' because internal or local addresses are not allowed for lead discovery.";
            return false;
        }

        uri = new Uri($"{parsed.Scheme}://{parsed.Host}");
        return true;
    }

    private static bool IsBlockedHost(string host)
    {
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IPAddress.TryParse(host, out var address) && IsPrivateAddress(address);
    }

    private static bool IsPrivateAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
            return true;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 10 ||
                bytes[0] == 127 ||
                bytes[0] == 192 && bytes[1] == 168 ||
                bytes[0] == 172 && bytes[1] is >= 16 and <= 31 ||
                bytes[0] == 169 && bytes[1] == 254;
        }

        return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast;
    }

    private static IEnumerable<Uri> CandidatePages(Uri rootUri)
    {
        yield return rootUri;

        foreach (var path in new[] { "/contact", "/contact-us", "/contacts", "/contacto", "/sobre", "/about" })
            yield return new Uri(rootUri, path);
    }

    private async Task<string> DownloadPageAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}");

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is not null && !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase) && !mediaType.Contains("text", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unsupported content type.");

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        var buffer = new char[8192];
        var builder = new StringBuilder();
        while (builder.Length < MaxPageChars)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, MaxPageChars - builder.Length)), cancellationToken);
            if (read == 0)
                break;

            builder.Append(buffer, 0, read);
        }

        return WebUtility.HtmlDecode(builder.ToString().Replace("mailto:", " ", StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> ExtractEmails(string html)
    {
        return EmailRegex.Matches(html)
            .Select(match => match.Value.Trim().TrimEnd('.', ',', ';', ':', ')', ']'))
            .Where(IsUsefulPublicEmail)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsUsefulPublicEmail(string email)
    {
        var lower = email.ToLowerInvariant();
        if (lower.Contains("no-reply") || lower.Contains("noreply") || lower.Contains("donotreply"))
            return false;

        if (lower.EndsWith(".png") || lower.EndsWith(".jpg") || lower.EndsWith(".jpeg") || lower.EndsWith(".webp") || lower.EndsWith(".svg"))
            return false;

        return true;
    }

    private static string CleanQueryPart(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        var cleaned = Regex.Replace(value, @"\s+", " ").Trim();
        return cleaned.Length <= 120 ? cleaned : cleaned[..120];
    }
}
