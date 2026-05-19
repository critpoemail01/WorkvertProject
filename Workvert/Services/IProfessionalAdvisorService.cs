namespace Workvert.Services;

public interface IProfessionalAdvisorService
{
    ProfessionalProfileAnalysis AnalyzeProfile(ProfessionalProfileRequest request);
    ServiceRequestAnalysis AnalyzeServiceRequest(ServiceRequestRequest request);
}

public record ProfessionalProfileRequest(
    string CurrentProfession,
    string Experience,
    string TechnicalSkills,
    string SoftSkills,
    string Tools,
    string Education,
    string Languages,
    string DesiredLocation,
    string WorkMode,
    string EngagementType,
    string CompensationGoal,
    string InterestAreas,
    string? PortfolioUrl,
    string? ProfilePhotoLabel);

public record ProfessionalProfileAnalysis(
    IReadOnlyList<string> SuggestedSkills,
    IReadOnlyList<OpportunityRecommendation> JobOpportunities,
    IReadOnlyList<FreelanceRecommendation> FreelanceOpportunities,
    CareerPlan CareerPlan,
    GeneratedProfileAssets GeneratedAssets,
    string PhotoUsageNote);

public record OpportunityRecommendation(
    string Title,
    string Organization,
    string Type,
    string Location,
    string WorkMode,
    int CompatibilityScore,
    IReadOnlyList<string> MatchReasons,
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingSkills,
    string SuggestedAction,
    string CompensationRange);

public record FreelanceRecommendation(
    string Service,
    string TargetClient,
    string PriceSuggestion,
    int CompatibilityScore,
    IReadOnlyList<string> RequiredSkills,
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingSkills,
    string ProposalAngle,
    string SuggestedAction);

public record CareerPlan(
    string Positioning,
    IReadOnlyList<string> SkillGaps,
    IReadOnlyList<string> RecommendedLearning,
    IReadOnlyList<string> CvImprovements,
    IReadOnlyList<string> NextSteps);

public record GeneratedProfileAssets(
    string CvSummary,
    string LinkedInHeadline,
    string ProfessionalBio,
    string CoverMessage,
    string FreelancePitch);

public record ServiceRequestRequest(
    string Description,
    string Location,
    string Budget,
    string Urgency,
    bool RemoteAllowed);

public record ServiceRequestAnalysis(
    string ServiceArea,
    string ProfessionalType,
    string Complexity,
    string DeliveryMode,
    string BudgetSignal,
    IReadOnlyList<string> RequiredSkills,
    IReadOnlyList<RecommendedProfessional> RecommendedProfessionals,
    string BriefForClient);

public record RecommendedProfessional(
    string Name,
    string ProfessionalArea,
    string Location,
    string Availability,
    string AveragePrice,
    int CompatibilityScore,
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> MatchReasons,
    string PortfolioSummary);
