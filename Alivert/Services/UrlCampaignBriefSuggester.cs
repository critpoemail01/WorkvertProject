using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Alivert.Services;

public sealed class UrlCampaignBriefSuggester : IUrlCampaignBriefSuggester
{
    private static readonly AppTypeRule[] Rules =
    [
        new("B2B SaaS / automation", ["crm", "automation", "workflow", "pipeline", "operations", "sales", "dashboard", "saas", "b2b"],
            "B2B teams, founders, operations managers and sales teams that need repeatable processes",
            "reduce manual work, organize recurring workflows and make acquisition or operations measurable",
            "demo bookings and trial signups",
            "clear, practical and business-focused",
            ["LinkedIn", "Instagram", "TikTok", "Facebook"]),

        new("AI product", ["ai", "artificial intelligence", "llm", "chatbot", "agent", "prompt", "machine learning", "automation"],
            "teams and professionals looking for AI-assisted productivity or automation",
            "turn manual tasks into faster AI-assisted workflows with clear business outcomes",
            "trial signups and product demos",
            "modern, educational and direct",
            ["LinkedIn", "X", "YouTube Shorts", "TikTok"]),

        new("Ecommerce / online store", ["shop", "store", "cart", "checkout", "buy", "shipping", "product", "ecommerce", "commerce"],
            "online shoppers and buyers interested in practical offers, product quality and fast purchase decisions",
            "make the product offer easy to understand, compare and buy online",
            "purchases and abandoned-cart recovery",
            "benefit-led, visual and conversion-focused",
            ["Instagram", "TikTok", "Facebook", "YouTube Shorts"]),

        new("Booking / appointments", ["booking", "appointment", "reservation", "schedule", "calendar", "availability", "clinic", "salon"],
            "people who need to book a service, appointment or reservation with less friction",
            "make booking faster, clearer and easier to complete from any device",
            "bookings and qualified enquiries",
            "helpful, local and action-oriented",
            ["Instagram", "Facebook", "TikTok", "LinkedIn"]),

        new("Clinics and healthcare", ["clinic", "clinica", "clínica", "doctor", "dentist", "medical", "health", "saude", "saúde", "consulta", "check-up", "appointment"],
            "local patients and families who need a trusted, easy way to book care",
            "make the first contact, check-up or appointment simple and trustworthy",
            "appointments and qualified patient enquiries",
            "trustworthy, clear and local",
            ["Instagram", "Facebook", "TikTok", "Email"]),

        new("Construction and renovation", ["construction", "construcao", "construção", "obra", "renovation", "remodel", "remodelacao", "remodelação", "builder", "orcamento", "orçamento"],
            "property owners, companies and managers planning works or renovations",
            "turn project interest into qualified quote requests with proof of completed work",
            "quote requests and site visits",
            "visual, trustworthy and practical",
            ["Instagram", "Facebook", "LinkedIn", "Email"]),

        new("Restaurants and hospitality", ["restaurant", "restaurante", "menu", "food", "comida", "reservation", "reserva", "bar", "cafe", "café", "hospitality"],
            "local customers, visitors and groups looking for a place to eat or book",
            "make menus, reservations and private events easy to discover and book",
            "reservations and event enquiries",
            "visual, local and appetite-led",
            ["Instagram", "Facebook", "TikTok", "Email"]),

        new("Real estate", ["real estate", "imobiliaria", "imobiliária", "property", "imovel", "imóvel", "house", "apartamento", "avaliacao", "avaliação"],
            "property owners, buyers and investors looking for trusted market guidance",
            "capture valuation requests, property listings and qualified buyer interest",
            "valuation requests and qualified property leads",
            "local, premium and trust-led",
            ["Facebook", "Instagram", "LinkedIn", "Email"]),

        new("Education / course platform", ["course", "learn", "training", "school", "academy", "lesson", "student", "education"],
            "learners, professionals and teams looking for structured training or practical skills",
            "help users learn faster with a clear path, useful content and measurable progress",
            "course registrations and lead capture",
            "educational, encouraging and practical",
            ["YouTube Shorts", "Instagram", "TikTok", "LinkedIn"]),

        new("Finance / invoicing", ["invoice", "payment", "billing", "finance", "accounting", "expense", "bank", "subscription"],
            "business owners, finance teams and operators who need cleaner financial workflows",
            "simplify money-related work, reduce admin and improve visibility over important numbers",
            "demo bookings and account signups",
            "trustworthy, precise and practical",
            ["LinkedIn", "Facebook", "Instagram", "X"]),

        new("Marketing / growth tool", ["marketing", "campaign", "lead", "growth", "social", "email", "sms", "ads", "conversion"],
            "founders, marketers and commercial teams that need more consistent acquisition",
            "plan, launch and measure campaigns across channels without scattered manual work",
            "trial signups and campaign launches",
            "growth-focused, direct and measurable",
            ["LinkedIn", "TikTok", "Instagram", "Facebook"]),

        new("Local services", ["service", "repair", "cleaning", "maintenance", "local", "quote", "estimate", "near me"],
            "local customers searching for reliable service providers and fast responses",
            "make it easy for nearby customers to understand the offer and request contact",
            "qualified enquiries and calls",
            "local, trustworthy and direct",
            ["Facebook", "Instagram", "TikTok", "LinkedIn"])
    ];

    private readonly HttpClient _httpClient;

    public UrlCampaignBriefSuggester(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UrlCampaignBriefSuggestion> SuggestAsync(string url, CancellationToken cancellationToken = default)
    {
        var normalizedUrl = NormalizeUrl(url);
        if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new InvalidOperationException("Use a valid http or https URL.");

        await GuardAgainstPrivateHostAsync(uri, cancellationToken);

        using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!string.IsNullOrWhiteSpace(mediaType) && !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The URL did not return an HTML page that can be analyzed.");

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        if (html.Length > 300_000)
            html = html[..300_000];

        return SuggestFromPage(uri, ExtractMetadata(html));
    }

    public static UrlCampaignBriefSuggestion SuggestFromPage(Uri uri, PageMetadata metadata)
    {
        var title = FirstMeaningful(metadata.OgTitle, metadata.ApplicationName, metadata.Title, DomainName(uri));
        var description = FirstMeaningful(metadata.OgDescription, metadata.Description, title);
        var body = $"{title} {description} {uri.Host} {uri.AbsolutePath}".ToLowerInvariant();
        var rule = Rules
            .Select(candidate => new { Rule = candidate, Score = candidate.Keywords.Count(keyword => KeywordMatches(body, keyword)) })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Rule.Name)
            .First();

        var selected = rule.Score == 0
            ? new AppTypeRule("General online application", [],
                "potential customers who need a clearer and faster way to solve this problem online",
                "explain the product clearly, remove friction and guide users to the next step",
                "signups and qualified enquiries",
                "clear, practical and benefit-led",
                ["LinkedIn", "Instagram", "Facebook", "TikTok"])
            : rule.Rule;

        var productName = Clip(CleanProductName(title, uri), 160);
        var evidence = Clip(description, 260);
        return new UrlCampaignBriefSuggestion(
            productName,
            Clip($"{productName} appears to be a {selected.Name.ToLowerInvariant()} based on its website. Main website signal: {evidence}", 400),
            Clip(selected.Audience, 700),
            Clip($"{selected.ValueProposition}. Website signal: {evidence}", 700),
            Clip(selected.Goal, 200),
            Clip(selected.Tone, 80),
            selected.Platforms,
            selected.Name,
            uri.ToString(),
            evidence);
    }

    public static PageMetadata ExtractMetadata(string html)
    {
        return new PageMetadata(
            CleanHtml(ExtractTitle(html)),
            CleanHtml(ExtractMeta(html, "name", "description")),
            CleanHtml(ExtractMeta(html, "property", "og:title")),
            CleanHtml(ExtractMeta(html, "property", "og:description")),
            CleanHtml(ExtractMeta(html, "name", "application-name")));
    }

    private static string NormalizeUrl(string url)
    {
        var trimmed = (url ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new InvalidOperationException("Add the application URL first.");

        return trimmed.Contains("://", StringComparison.Ordinal) ? trimmed : $"https://{trimmed}";
    }

    private static async Task GuardAgainstPrivateHostAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (IPAddress.TryParse(uri.Host, out var parsed))
        {
            if (IsPrivateAddress(parsed))
                throw new InvalidOperationException("Use a public application URL.");

            return;
        }

        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use a public application URL.");

        var addresses = await Dns.GetHostAddressesAsync(uri.Host, cancellationToken);
        if (addresses.Any(IsPrivateAddress))
            throw new InvalidOperationException("Use a public application URL.");
    }

    private static bool IsPrivateAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
            return true;

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal;

        var bytes = address.GetAddressBytes();
        return bytes[0] == 10 ||
            bytes[0] == 127 ||
            (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
            (bytes[0] == 192 && bytes[1] == 168) ||
            (bytes[0] == 169 && bytes[1] == 254);
    }

    private static string ExtractTitle(string html)
    {
        var match = Regex.Match(html, "<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static string ExtractMeta(string html, string attributeName, string attributeValue)
    {
        var pattern = $@"<meta\b(?=[^>]*\b{Regex.Escape(attributeName)}\s*=\s*['""]{Regex.Escape(attributeValue)}['""])(?=[^>]*\bcontent\s*=\s*(['""])(.*?)\1)[^>]*>";
        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups[2].Value : string.Empty;
    }

    private static string CleanHtml(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decoded = WebUtility.HtmlDecode(value);
        var withoutTags = Regex.Replace(decoded, "<.*?>", " ");
        return Regex.Replace(withoutTags, @"\s+", " ").Trim();
    }

    private static string FirstMeaningful(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "Online application";
    }

    private static string CleanProductName(string title, Uri uri)
    {
        var cleaned = title;
        foreach (var separator in new[] { " | ", " - ", " · ", " — ", " – " })
        {
            var index = cleaned.IndexOf(separator, StringComparison.Ordinal);
            if (index > 1)
            {
                cleaned = cleaned[..index];
                break;
            }
        }

        cleaned = cleaned.Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? DomainName(uri) : cleaned;
    }

    private static string DomainName(Uri uri)
    {
        var host = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host;
        var first = host.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Online application";
        return string.Join(" ", Regex.Split(first, "[-_]")).Trim();
    }

    private static string Clip(string value, int maxLength)
    {
        if (value.Length <= maxLength)
            return value;

        return value[..Math.Max(0, maxLength - 3)].TrimEnd() + "...";
    }

    private static bool KeywordMatches(string body, string keyword)
    {
        return keyword.Length <= 3
            ? Regex.IsMatch(body, $@"\b{Regex.Escape(keyword)}\b", RegexOptions.IgnoreCase)
            : body.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    public sealed record PageMetadata(
        string Title,
        string Description,
        string OgTitle,
        string OgDescription,
        string ApplicationName);

    private sealed record AppTypeRule(
        string Name,
        IReadOnlyList<string> Keywords,
        string Audience,
        string ValueProposition,
        string Goal,
        string Tone,
        IReadOnlyList<string> Platforms);
}
