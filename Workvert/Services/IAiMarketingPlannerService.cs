using Workvert.Models;

namespace Workvert.Services;

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
    string? EmailAudience,
    AiAudienceLocation Location,
    CompanyLearningProfile? CompanyLearning = null);

public sealed record AiAudienceLocation(
    string Scope,
    string? Country,
    string? City,
    double? Latitude,
    double? Longitude,
    int? RadiusKm)
{
    public string Summary
    {
        get
        {
            if (Scope.Equals("Country", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(Country))
                return Country.Trim();

            if (Scope.Equals("City", StringComparison.OrdinalIgnoreCase))
            {
                var place = string.Join(", ", new[] { City, Country }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
                if (string.IsNullOrWhiteSpace(place))
                    place = "selected city";

                return RadiusKm is > 0 ? $"{place}, within {RadiusKm} km" : place;
            }

            return "worldwide";
        }
    }
}

public sealed record AiMarketingPlanDraft(
    IReadOnlyList<MarketingPostSuggestion> Posts,
    IReadOnlyList<MarketingEmailSuggestion> Emails,
    IReadOnlyList<MarketingLeadSuggestion> Leads,
    MarketingLandingPage LandingPage,
    string BusinessDna);
