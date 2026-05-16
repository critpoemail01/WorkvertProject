namespace Dealvert.Services;

public interface IUrlCampaignBriefSuggester
{
    Task<UrlCampaignBriefSuggestion> SuggestAsync(string url, CancellationToken cancellationToken = default);
}

public sealed record UrlCampaignBriefSuggestion(
    string ProductName,
    string CompanyOrIdea,
    string TargetAudience,
    string ValueProposition,
    string CampaignGoal,
    string Tone,
    IReadOnlyList<string> Platforms,
    string DetectedApplicationType,
    string SourceUrl,
    string Evidence);
