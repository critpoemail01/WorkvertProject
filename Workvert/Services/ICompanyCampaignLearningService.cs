namespace Workvert.Services;

public interface ICompanyCampaignLearningService
{
    Task<CompanyLearningProfile> BuildAsync(
        string userId,
        string? productName,
        string? productUrl,
        string? companyOrIdea,
        int? excludePlanId = null,
        CancellationToken cancellationToken = default);
}

public sealed record CompanyLearningProfile(
    bool HasData,
    string CompanyKey,
    int CampaignsAnalyzed,
    int LeadsGenerated,
    string Summary,
    IReadOnlyList<string> WinningPatterns,
    IReadOnlyList<string> RecommendedAdaptations,
    IReadOnlyList<string> PreferredPlatforms,
    string PreferredPostStyle,
    string PreferredCta,
    string EmailStyle,
    string LandingPageAdvice,
    string RecommendedCampaignBrief)
{
    public static CompanyLearningProfile Empty(string companyKey = "Current company")
    {
        return new CompanyLearningProfile(
            false,
            companyKey,
            0,
            0,
            "No previous campaign results for this company yet.",
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            "No winning post style yet",
            "No winning CTA yet",
            "No email pattern yet",
            "No landing page pattern yet",
            "Run the first product watch, capture signals and Workvert will adapt the next one.");
    }
}
