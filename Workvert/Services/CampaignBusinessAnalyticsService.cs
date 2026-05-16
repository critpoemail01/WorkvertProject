using Workvert.Models;
using System.Globalization;

namespace Workvert.Services;

public sealed class CampaignBusinessAnalyticsService : ICampaignBusinessAnalyticsService
{
    private const decimal CreditCostReference = 1.00m;

    public CampaignBusinessReport BuildCampaignReport(MarketingPlan plan)
    {
        var posts = SelectReportPosts(plan).ToList();
        var emails = SelectReportEmails(plan).ToList();
        var sourceLeadCounts = CountCapturedLeadSources(plan.LandingPage);
        var capturedLeads = plan.LandingPage?.Leads.Count ?? 0;
        var estimatedLeads = posts.Sum(x => x.EstimatedConversions) + emails.Sum(x => x.EstimatedConversions);
        var leads = capturedLeads > 0 ? capturedLeads : estimatedLeads;
        var reach = posts.Sum(x => x.EstimatedReach) + emails.Sum(x => x.EstimatedReach) + (plan.LandingPage?.ViewCount ?? 0);
        var clicks = posts.Sum(x => x.EstimatedInteractions) + emails.Sum(x => x.EstimatedInteractions) + (plan.LandingPage?.ViewCount ?? 0);
        var costPerLead = CalculateCostPerLead(plan, leads);
        var channels = BuildChannelMetrics(posts, emails, plan.LandingPage, sourceLeadCounts, capturedLeads > 0);
        var topChannel = channels
            .OrderByDescending(x => x.Leads)
            .ThenByDescending(x => x.Clicks)
            .FirstOrDefault();
        var topPost = posts
            .OrderByDescending(x => x.EstimatedConversions)
            .ThenByDescending(x => x.EstimatedInteractions)
            .FirstOrDefault();
        var bestEmail = emails
            .OrderByDescending(x => x.EstimatedInteractions)
            .ThenByDescending(x => x.EstimatedReach)
            .FirstOrDefault();

        return new CampaignBusinessReport(
            reach,
            clicks,
            leads,
            FormatRate(leads, clicks),
            FormatCostPerLead(costPerLead),
            topChannel?.Channel ?? "No channel data yet",
            topChannel?.Leads ?? 0,
            topPost?.Title ?? "No post performance yet",
            topPost is null ? "Publish social content with UTM links to identify the winning post." : $"{topPost.Platform}: {topPost.EstimatedConversions} leads, {topPost.EstimatedInteractions} clicks",
            bestEmail?.Subject ?? "No email performance yet",
            bestEmail is null ? "Send an approved email sequence to measure opens and clicks." : $"{bestEmail.EstimatedReach} opens, {bestEmail.EstimatedInteractions} clicks, {bestEmail.EstimatedConversions} leads",
            BuildProfitabilitySummary(leads, costPerLead),
            BuildNextAction(clicks, leads, topChannel?.Channel, topPost?.Title),
            channels);
    }

    public CampaignPortfolioBusinessReport BuildPortfolioReport(IEnumerable<MarketingPlan> plans)
    {
        var reports = plans
            .Select(plan => new
            {
                Plan = plan,
                Report = BuildCampaignReport(plan)
            })
            .ToList();

        var reach = reports.Sum(x => x.Report.Reach);
        var clicks = reports.Sum(x => x.Report.Clicks);
        var leads = reports.Sum(x => x.Report.Leads);
        var channels = reports
            .SelectMany(x => x.Report.Channels)
            .GroupBy(x => x.Channel)
            .Select(group =>
            {
                var channelReach = group.Sum(x => x.Reach);
                var channelClicks = group.Sum(x => x.Clicks);
                var channelLeads = group.Sum(x => x.Leads);
                return new BusinessChannelMetric(
                    group.Key,
                    channelReach,
                    channelClicks,
                    channelLeads,
                    FormatRate(channelLeads, channelClicks),
                    BuildChannelSignal(channelLeads, channelClicks));
            })
            .OrderByDescending(x => x.Leads)
            .ThenByDescending(x => x.Clicks)
            .ToList();

        var campaignRows = reports
            .Select(x => new BusinessCampaignMetric(
                x.Plan.Id,
                x.Plan.ProductName,
                x.Report.Clicks,
                x.Report.Leads,
                x.Report.ConversionRateLabel,
                x.Report.CostPerLeadLabel,
                x.Report.TopLeadChannel,
                x.Report.RecommendedNextAction))
            .OrderByDescending(x => x.Leads)
            .ThenByDescending(x => x.Clicks)
            .Take(8)
            .ToList();

        var topChannel = channels.FirstOrDefault();
        var bestPost = reports
            .SelectMany(x => SelectReportPosts(x.Plan).Select(post => new { x.Plan.ProductName, Post = post }))
            .OrderByDescending(x => x.Post.EstimatedConversions)
            .ThenByDescending(x => x.Post.EstimatedInteractions)
            .FirstOrDefault();
        var bestEmail = reports
            .SelectMany(x => SelectReportEmails(x.Plan).Select(email => new { x.Plan.ProductName, Email = email }))
            .OrderByDescending(x => x.Email.EstimatedInteractions)
            .ThenByDescending(x => x.Email.EstimatedReach)
            .FirstOrDefault();
        var mostProfitable = reports
            .Where(x => x.Report.Leads > 0)
            .OrderBy(x => CalculateCostPerLead(x.Plan, x.Report.Leads))
            .ThenByDescending(x => x.Report.Leads)
            .FirstOrDefault();

        return new CampaignPortfolioBusinessReport(
            reach,
            clicks,
            leads,
            FormatRate(leads, clicks),
            FormatPortfolioCostPerLead(reports.Select(x => x.Plan), leads),
            topChannel?.Channel ?? "No channel data yet",
            topChannel?.Leads ?? 0,
            bestPost?.Post.Title ?? "No post performance yet",
            bestPost is null ? "Approve and publish posts with UTM links." : $"{bestPost.ProductName} / {bestPost.Post.Platform}: {bestPost.Post.EstimatedConversions} leads, {bestPost.Post.EstimatedInteractions} clicks",
            bestEmail?.Email.Subject ?? "No email performance yet",
            bestEmail is null ? "Send an approved email sequence to measure opens and clicks." : $"{bestEmail.ProductName}: {bestEmail.Email.EstimatedReach} opens, {bestEmail.Email.EstimatedInteractions} clicks",
            mostProfitable?.Plan.ProductName ?? "No profitable campaign yet",
            mostProfitable is null ? "Capture at least one lead to calculate cost per lead." : $"{mostProfitable.Report.Leads} leads at {mostProfitable.Report.CostPerLeadLabel}",
            BuildPortfolioNextAction(reports.Select(x => x.Report).ToList(), topChannel?.Channel),
            channels,
            campaignRows);
    }

    private static IEnumerable<MarketingPostSuggestion> SelectReportPosts(MarketingPlan plan)
    {
        var livePosts = plan.Posts.Where(x => x.Status == "Published").ToList();
        return livePosts.Count > 0 ? livePosts : plan.Posts;
    }

    private static IEnumerable<MarketingEmailSuggestion> SelectReportEmails(MarketingPlan plan)
    {
        var sentEmails = plan.Emails.Where(x => x.Status == "Sent").ToList();
        return sentEmails.Count > 0 ? sentEmails : plan.Emails;
    }

    private static IReadOnlyList<BusinessChannelMetric> BuildChannelMetrics(
        IReadOnlyList<MarketingPostSuggestion> posts,
        IReadOnlyList<MarketingEmailSuggestion> emails,
        MarketingLandingPage? landingPage,
        IReadOnlyDictionary<string, int> sourceLeadCounts,
        bool useCapturedAttribution)
    {
        var channels = posts
            .GroupBy(x => NormalizeChannel(x.Platform))
            .Select(group =>
            {
                var reach = group.Sum(x => x.EstimatedReach);
                var clicks = group.Sum(x => x.EstimatedInteractions);
                var leads = useCapturedAttribution
                    ? sourceLeadCounts.GetValueOrDefault(group.Key)
                    : group.Sum(x => x.EstimatedConversions);
                return new BusinessChannelMetric(group.Key, reach, clicks, leads, FormatRate(leads, clicks), BuildChannelSignal(leads, clicks));
            })
            .ToList();

        if (emails.Count > 0)
        {
            var reach = emails.Sum(x => x.EstimatedReach);
            var clicks = emails.Sum(x => x.EstimatedInteractions);
            var leads = useCapturedAttribution
                ? sourceLeadCounts.GetValueOrDefault("Email")
                : emails.Sum(x => x.EstimatedConversions);
            channels.Add(new BusinessChannelMetric("Email", reach, clicks, leads, FormatRate(leads, clicks), BuildChannelSignal(leads, clicks)));
        }

        if (landingPage is not null && (landingPage.ViewCount > 0 || landingPage.Leads.Count > 0))
        {
            var landingLeads = useCapturedAttribution
                ? sourceLeadCounts.GetValueOrDefault("Landing page")
                : landingPage.Leads.Count;

            channels.Add(new BusinessChannelMetric(
                "Landing page",
                landingPage.ViewCount,
                landingPage.ViewCount,
                landingLeads,
                FormatRate(landingLeads, landingPage.ViewCount),
                BuildChannelSignal(landingLeads, landingPage.ViewCount)));
        }

        return channels
            .GroupBy(x => x.Channel)
            .Select(group =>
            {
                var reach = group.Sum(x => x.Reach);
                var clicks = group.Sum(x => x.Clicks);
                var leads = group.Sum(x => x.Leads);
                return new BusinessChannelMetric(group.Key, reach, clicks, leads, FormatRate(leads, clicks), BuildChannelSignal(leads, clicks));
            })
            .OrderByDescending(x => x.Leads)
            .ThenByDescending(x => x.Clicks)
            .ToList();
    }

    private static Dictionary<string, int> CountCapturedLeadSources(MarketingLandingPage? landingPage)
    {
        if (landingPage is null)
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        return landingPage.Leads
            .GroupBy(lead => NormalizeLeadSource(lead.Source), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeLeadSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return "Landing page";

        var lower = source.ToLowerInvariant();
        if (lower.Contains("linkedin")) return "LinkedIn";
        if (lower.Contains("instagram")) return "Instagram";
        if (lower.Contains("facebook")) return "Facebook";
        if (lower.Contains("tiktok")) return "TikTok";
        if (lower.Contains("youtube")) return "YouTube Shorts";
        if (lower.Contains("email")) return "Email";
        if (lower.Contains("google")) return "Google Business";
        return "Landing page";
    }

    private static string NormalizeChannel(string channel)
    {
        var trimmed = channel.Trim();
        return trimmed.ToLowerInvariant() switch
        {
            "linkedin" => "LinkedIn",
            "instagram" => "Instagram",
            "facebook" => "Facebook",
            "tiktok" => "TikTok",
            "youtube shorts" => "YouTube Shorts",
            "google business" => "Google Business",
            "email" => "Email",
            "x" => "X",
            _ => string.IsNullOrWhiteSpace(trimmed) ? "Unknown" : trimmed
        };
    }

    private static decimal? CalculateCostPerLead(MarketingPlan plan, int leads)
    {
        if (leads <= 0)
            return null;

        var creditUnits = Math.Max(1, CampaignCreditUsage.CountPlatformUnits(plan.Platforms));
        return creditUnits * CreditCostReference / leads;
    }

    private static string FormatPortfolioCostPerLead(IEnumerable<MarketingPlan> plans, int leads)
    {
        if (leads <= 0)
            return "No leads yet";

        var creditUnits = plans.Sum(plan => Math.Max(1, CampaignCreditUsage.CountPlatformUnits(plan.Platforms)));
        return $"{(creditUnits * CreditCostReference / leads).ToString("0.00", CultureInfo.InvariantCulture)} credits/lead";
    }

    private static string FormatCostPerLead(decimal? value)
    {
        return value is null ? "No leads yet" : $"{value.Value.ToString("0.00", CultureInfo.InvariantCulture)} credits/lead";
    }

    private static string FormatRate(int numerator, int denominator)
    {
        return denominator <= 0
            ? "0.0%"
            : $"{((decimal)numerator / denominator * 100).ToString("0.0", CultureInfo.InvariantCulture)}%";
    }

    private static string BuildProfitabilitySummary(int leads, decimal? costPerLead)
    {
        return leads <= 0 || costPerLead is null
            ? "No lead cost yet"
            : $"{leads} leads at {FormatCostPerLead(costPerLead)}";
    }

    private static string BuildChannelSignal(int leads, int clicks)
    {
        if (leads > 0)
            return $"{leads} leads from {clicks} clicks";

        if (clicks > 0)
            return "Clicks without leads. Improve CTA or form.";

        return "No traffic yet";
    }

    private static string BuildNextAction(int clicks, int leads, string? topChannel, string? topPost)
    {
        if (clicks <= 0)
            return "Publish approved assets and send all traffic to the landing page.";

        if (leads <= 0)
            return "Keep the best-clicking channel, then revise the CTA and landing form.";

        if (!string.IsNullOrWhiteSpace(topChannel))
            return $"Repeat {topChannel} with a variation of the winning CTA.";

        return !string.IsNullOrWhiteSpace(topPost)
            ? $"Repeat the winning post angle: {topPost}."
            : "Create the next campaign from the highest converting channel.";
    }

    private static string BuildPortfolioNextAction(IReadOnlyList<CampaignBusinessReport> reports, string? topChannel)
    {
        if (reports.Count == 0)
            return "Create the first campaign with posts, emails, landing page and UTM tracking.";

        if (reports.Sum(x => x.Clicks) <= 0)
            return "Schedule approved campaigns so analytics can start collecting traffic.";

        if (reports.Sum(x => x.Leads) <= 0)
            return "Improve the landing CTA before increasing volume.";

        return string.IsNullOrWhiteSpace(topChannel)
            ? "Create a follow-up campaign from the strongest conversion angle."
            : $"Repeat the best campaign on {topChannel} and test one new CTA.";
    }
}
