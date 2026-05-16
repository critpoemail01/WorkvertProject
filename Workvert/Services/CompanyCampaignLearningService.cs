using Workvert.Data;
using Workvert.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Workvert.Services;

public sealed class CompanyCampaignLearningService : ICompanyCampaignLearningService
{
    private readonly ApplicationDbContext _db;
    private readonly ICampaignBusinessAnalyticsService _analytics;

    public CompanyCampaignLearningService(ApplicationDbContext db, ICampaignBusinessAnalyticsService analytics)
    {
        _db = db;
        _analytics = analytics;
    }

    public async Task<CompanyLearningProfile> BuildAsync(
        string userId,
        string? productName,
        string? productUrl,
        string? companyOrIdea,
        int? excludePlanId = null,
        CancellationToken cancellationToken = default)
    {
        var target = BuildIdentity(productName, productUrl, companyOrIdea);
        if (!target.HasIdentity)
            return CompanyLearningProfile.Empty();

        var userPlans = await _db.MarketingPlans
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Include(x => x.Posts)
            .Include(x => x.Emails)
            .Include(x => x.LandingPage)
            .ThenInclude(x => x!.Leads)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(80)
            .ToListAsync(cancellationToken);

        var matchingPlans = userPlans
            .Where(plan => excludePlanId is null || plan.Id != excludePlanId.Value)
            .Where(plan => Matches(target, BuildIdentity(plan.ProductName, plan.ProductUrl, plan.CompanyOrIdea)))
            .ToList();

        if (matchingPlans.Count == 0)
            return CompanyLearningProfile.Empty(target.Label);

        var reports = matchingPlans
            .Select(plan => new
            {
                Plan = plan,
                Report = _analytics.BuildCampaignReport(plan)
            })
            .Where(x => x.Report.Clicks > 0 || x.Report.Leads > 0 || x.Plan.LandingPage?.Leads.Count > 0)
            .ToList();

        if (reports.Count == 0)
            return CompanyLearningProfile.Empty(target.Label);

        var channelRows = reports
            .SelectMany(x => x.Report.Channels)
            .Where(x => !x.Channel.Equals("Landing page", StringComparison.OrdinalIgnoreCase))
            .GroupBy(x => x.Channel, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Channel = group.Key,
                Reach = group.Sum(x => x.Reach),
                Clicks = group.Sum(x => x.Clicks),
                Leads = group.Sum(x => x.Leads)
            })
            .OrderByDescending(x => x.Leads)
            .ThenByDescending(x => x.Clicks)
            .ToList();

        var preferredPlatforms = channelRows
            .Where(x => !x.Channel.Equals("Email", StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .Select(x => x.Channel)
            .ToList();
        var topChannel = channelRows.FirstOrDefault();
        var topPost = SelectBestPost(reports.Select(x => x.Plan));
        var bestEmail = SelectBestEmail(reports.Select(x => x.Plan));
        var capturedLeads = reports
            .SelectMany(x => x.Plan.LandingPage?.Leads ?? Enumerable.Empty<MarketingLandingLead>())
            .ToList();
        var leads = reports.Sum(x => x.Report.Leads);
        var preferredPostStyle = InferPostStyle(topPost);
        var preferredCta = InferPreferredCta(topPost, reports.Select(x => x.Plan));
        var emailStyle = InferEmailStyle(bestEmail);
        var landingAdvice = InferLandingPageAdvice(reports.Select(x => x.Plan));
        var bestDays = InferBestDays(capturedLeads, topPost);
        var winningPatterns = new List<string>
        {
            $"{preferredPostStyle} performed best",
            $"CTA: {preferredCta}",
            $"{topChannel?.Channel ?? "No channel"} is the strongest channel",
            bestDays,
            emailStyle,
            landingAdvice
        };

        var adaptations = new List<string>
        {
            $"Prioritize {topChannel?.Channel ?? "the channel with the most leads"} before adding more networks.",
            $"Use the CTA '{preferredCta}' as the primary action.",
            $"Keep {emailStyle.ToLowerInvariant()} in the follow-up sequence.",
            landingAdvice
        };

        if (preferredPlatforms.Count == 0 && topChannel is not null)
            preferredPlatforms.Add(topChannel.Channel);

        return new CompanyLearningProfile(
            true,
            target.Label,
            reports.Count,
            leads,
            $"This company converts better with {preferredPostStyle.ToLowerInvariant()}, '{preferredCta}' CTAs, {emailStyle.ToLowerInvariant()} and {landingAdvice.ToLowerInvariant()}.",
            winningPatterns,
            adaptations,
            preferredPlatforms,
            preferredPostStyle,
            preferredCta,
            emailStyle,
            landingAdvice,
            $"Based on previous campaigns, recommend a {preferredPostStyle.ToLowerInvariant()} campaign on {topChannel?.Channel ?? "the strongest channel"} with {emailStyle.ToLowerInvariant()} follow-up.");
    }

    private static MarketingPostSuggestion? SelectBestPost(IEnumerable<MarketingPlan> plans)
    {
        var posts = plans
            .SelectMany(plan =>
            {
                var published = plan.Posts.Where(x => x.Status == "Published").ToList();
                return published.Count > 0 ? published : plan.Posts;
            })
            .ToList();

        return posts
            .OrderByDescending(x => x.EstimatedConversions)
            .ThenByDescending(x => x.EstimatedInteractions)
            .FirstOrDefault();
    }

    private static MarketingEmailSuggestion? SelectBestEmail(IEnumerable<MarketingPlan> plans)
    {
        var emails = plans
            .SelectMany(plan =>
            {
                var sent = plan.Emails.Where(x => x.Status == "Sent").ToList();
                return sent.Count > 0 ? sent : plan.Emails;
            })
            .ToList();

        return emails
            .OrderByDescending(x => x.EstimatedInteractions)
            .ThenByDescending(x => x.EstimatedConversions)
            .FirstOrDefault();
    }

    private static string InferPostStyle(MarketingPostSuggestion? post)
    {
        if (post is null)
            return "educational posts";

        var text = $"{post.Title} {post.Hook} {post.Caption} {post.CreativeBrief}".ToLowerInvariant();
        if (ContainsAny(text, "case", "proof", "result", "testimonial", "customer"))
            return "case study posts";
        if (ContainsAny(text, "how", "use case", "workflow", "guide", "learn", "educat"))
            return "educational posts";
        if (ContainsAny(text, "demo", "show", "watch", "product in action"))
            return "product demo posts";
        if (ContainsAny(text, "before", "after", "old way", "new way"))
            return "before-and-after posts";
        return "direct problem-solution posts";
    }

    private static string InferPreferredCta(MarketingPostSuggestion? topPost, IEnumerable<MarketingPlan> plans)
    {
        var cta = topPost?.CallToAction;
        if (string.IsNullOrWhiteSpace(cta) || cta.StartsWith("Visit http", StringComparison.OrdinalIgnoreCase))
        {
            cta = plans
                .Select(x => x.LandingPage?.PrimaryCallToAction)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        }

        if (string.IsNullOrWhiteSpace(cta))
            return "Book a demo";

        if (cta.Contains("demo", StringComparison.OrdinalIgnoreCase))
            return cta;

        if (cta.Contains("request", StringComparison.OrdinalIgnoreCase))
            return cta;

        return cta.Length <= 80 ? cta : cta[..77].TrimEnd() + "...";
    }

    private static string InferEmailStyle(MarketingEmailSuggestion? email)
    {
        if (email is null)
            return "short emails";

        var bodyLength = email.Body.Length;
        if (bodyLength <= 650)
            return "short emails";

        if (email.Body.Contains("proof", StringComparison.OrdinalIgnoreCase) ||
            email.Body.Contains("trust", StringComparison.OrdinalIgnoreCase))
        {
            return "proof-led emails";
        }

        return "educational emails";
    }

    private static string InferLandingPageAdvice(IEnumerable<MarketingPlan> plans)
    {
        var landingPages = plans
            .Select(x => x.LandingPage)
            .Where(x => x is not null)
            .Cast<MarketingLandingPage>()
            .ToList();

        var views = landingPages.Sum(x => x.ViewCount);
        var leads = landingPages.Sum(x => x.Leads.Count);
        if (views <= 0)
            return "landing pages with a simple form";

        var rate = (decimal)leads / views;
        return rate >= 0.05m
            ? "landing pages with a simple form"
            : "shorter landing pages with one CTA and fewer form fields";
    }

    private static string InferBestDays(IReadOnlyList<MarketingLandingLead> capturedLeads, MarketingPostSuggestion? topPost)
    {
        var leadDays = capturedLeads
            .GroupBy(x => x.CreatedAtUtc.DayOfWeek)
            .OrderByDescending(x => x.Count())
            .Take(2)
            .Select(x => FormatDay(x.Key))
            .ToList();

        if (leadDays.Count > 0)
            return $"Best days: {string.Join(" and ", leadDays)}";

        return topPost is null
            ? "Best days: not enough data yet"
            : $"Best days: {FormatDay(topPost.ScheduledForUtc.DayOfWeek)}";
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private static string FormatDay(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => "Mondays",
            DayOfWeek.Tuesday => "Tuesdays",
            DayOfWeek.Wednesday => "Wednesdays",
            DayOfWeek.Thursday => "Thursdays",
            DayOfWeek.Friday => "Fridays",
            DayOfWeek.Saturday => "Saturdays",
            _ => "Sundays"
        };
    }

    private static bool Matches(CompanyIdentity target, CompanyIdentity candidate)
    {
        if (!target.HasIdentity || !candidate.HasIdentity)
            return false;

        if (!string.IsNullOrWhiteSpace(target.Host) && !string.IsNullOrWhiteSpace(candidate.Host))
            return target.Host.Equals(candidate.Host, StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(target.NameKey) && target.NameKey == candidate.NameKey)
            return true;

        var overlap = target.Tokens.Intersect(candidate.Tokens, StringComparer.OrdinalIgnoreCase).Count();
        return overlap >= 2;
    }

    private static CompanyIdentity BuildIdentity(string? productName, string? productUrl, string? companyOrIdea)
    {
        var host = NormalizeHost(productUrl);
        var label = !string.IsNullOrWhiteSpace(host)
            ? host
            : FirstNonEmpty(productName, companyOrIdea, "Current company");
        var nameKey = NormalizeName(FirstNonEmpty(productName, companyOrIdea, host));
        var tokens = Tokenize($"{productName} {companyOrIdea} {host}");
        return new CompanyIdentity(host, nameKey, tokens, label);
    }

    private static string? NormalizeHost(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        return uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? uri.Host[4..].ToLowerInvariant()
            : uri.Host.ToLowerInvariant();
    }

    private static string NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.ToLowerInvariant())
            builder.Append(char.IsLetterOrDigit(ch) ? ch : ' ');

        return string.Join(" ", builder
            .ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 2)
            .Take(4));
    }

    private static IReadOnlyList<string> Tokenize(string value)
    {
        return NormalizeName(value)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }

    private sealed record CompanyIdentity(string? Host, string NameKey, IReadOnlyList<string> Tokens, string Label)
    {
        public bool HasIdentity => !string.IsNullOrWhiteSpace(Host) || !string.IsNullOrWhiteSpace(NameKey);
    }
}
