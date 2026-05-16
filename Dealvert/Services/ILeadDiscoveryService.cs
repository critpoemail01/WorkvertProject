namespace Dealvert.Services;

public interface ILeadDiscoveryService
{
    Task<LeadDiscoveryResult> DiscoverAsync(LeadDiscoveryRequest request, CancellationToken cancellationToken = default);
}

public sealed record LeadDiscoveryRequest(
    string TargetAudience,
    string CampaignGoal,
    string LocationSummary,
    string? CompanyWebsiteUrls,
    string? ProductContext = null,
    bool SearchOnline = false);

public sealed record LeadDiscoveryResult(
    IReadOnlyList<LeadSearchQuery> SearchQueries,
    IReadOnlyList<DiscoveredLeadEmail> Emails,
    IReadOnlyList<string> Warnings);

public sealed record LeadSearchQuery(string Label, string Url);

public sealed record DiscoveredLeadEmail(string Email, string SourceUrl, string SourceHost);
