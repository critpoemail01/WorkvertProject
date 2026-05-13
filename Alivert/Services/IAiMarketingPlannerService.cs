using Alivert.Models;

namespace Alivert.Services;

public interface IAiMarketingPlannerService
{
    AiMarketingPlanDraft Generate(AiMarketingPlanRequest request);
}

public sealed record AiMarketingPlanRequest(
    string ProductName,
    string? ProductUrl,
    string CompanyOrIdea,
    string TargetAudience,
    string ValueProposition,
    string CampaignGoal,
    string Tone,
    IReadOnlyList<string> Platforms,
    DateOnly StartDate,
    DateOnly EndDate,
    string Frequency,
    string? EmailAudience);

public sealed record AiMarketingPlanDraft(
    IReadOnlyList<MarketingPostSuggestion> Posts,
    IReadOnlyList<MarketingEmailSuggestion> Emails,
    IReadOnlyList<MarketingLeadSuggestion> Leads);
