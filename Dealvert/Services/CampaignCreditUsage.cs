namespace Dealvert.Services;

public static class CampaignCreditUsage
{
    public static bool IsActiveMarketingPlanStatus(string? status)
    {
        return string.Equals(status, "Scheduled", StringComparison.OrdinalIgnoreCase);
    }

    public static int CountPlatformUnits(string? platforms)
    {
        if (string.IsNullOrWhiteSpace(platforms))
            return 0;

        return platforms
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(platform => !string.IsNullOrWhiteSpace(platform))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
    }
}
