namespace Alivert.Services;

public interface ICampaignLibraryService
{
    IReadOnlyList<SectorCampaignRecommendation> Recommend(CampaignLibraryRequest request, int maxResults = 3);
    SectorCampaignRecommendation? Find(string key);
}

public sealed record CampaignLibraryRequest(
    string? ProductName,
    string? CompanyOrIdea,
    string? TargetAudience,
    string? ValueProposition,
    string? CampaignGoal,
    string? DetectedApplicationType);

public sealed record SectorCampaignRecommendation(
    string Key,
    string Sector,
    string Title,
    string Goal,
    string Offer,
    string Audience,
    IReadOnlyList<string> Platforms,
    int DurationDays,
    string Frequency,
    string Strategy,
    string CreativeAngle,
    string LandingPageBrief,
    string FormBrief,
    string FollowUpBrief,
    string ReportFocus);
