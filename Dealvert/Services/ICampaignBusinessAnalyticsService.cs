using Dealvert.Models;

namespace Dealvert.Services;

public interface ICampaignBusinessAnalyticsService
{
    CampaignBusinessReport BuildCampaignReport(MarketingPlan plan);
    CampaignPortfolioBusinessReport BuildPortfolioReport(IEnumerable<MarketingPlan> plans);
}

public sealed record CampaignBusinessReport(
    int Reach,
    int Clicks,
    int Leads,
    string ConversionRateLabel,
    string CostPerLeadLabel,
    string TopLeadChannel,
    int TopLeadChannelLeads,
    string TopLeadPost,
    string TopLeadPostDetail,
    string BestEmail,
    string BestEmailDetail,
    string MostProfitableSummary,
    string RecommendedNextAction,
    IReadOnlyList<BusinessChannelMetric> Channels);

public sealed record CampaignPortfolioBusinessReport(
    int Reach,
    int Clicks,
    int Leads,
    string ConversionRateLabel,
    string CostPerLeadLabel,
    string TopLeadChannel,
    int TopLeadChannelLeads,
    string TopLeadPost,
    string TopLeadPostDetail,
    string BestEmail,
    string BestEmailDetail,
    string MostProfitableCampaign,
    string MostProfitableCampaignDetail,
    string RecommendedNextAction,
    IReadOnlyList<BusinessChannelMetric> Channels,
    IReadOnlyList<BusinessCampaignMetric> Campaigns);

public sealed record BusinessChannelMetric(
    string Channel,
    int Reach,
    int Clicks,
    int Leads,
    string ConversionRateLabel,
    string Signal);

public sealed record BusinessCampaignMetric(
    int Id,
    string Campaign,
    int Clicks,
    int Leads,
    string ConversionRateLabel,
    string CostPerLeadLabel,
    string BestChannel,
    string NextAction);
