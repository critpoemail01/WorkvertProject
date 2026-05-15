namespace Alivert.Models;

public static class AlertRuleTypeExtensions
{
    public static bool RequiresTechnicalIndicators(this AlertRuleType ruleType)
    {
        return ruleType is AlertRuleType.RsiBelow
            or AlertRuleType.RsiAbove
            or AlertRuleType.RsiOversoldEmaCrossUp
            or AlertRuleType.RsiOverboughtEmaCrossDown
            or AlertRuleType.EmaCrossUp
            or AlertRuleType.EmaCrossDown;
    }

    public static bool UsesPriceZone(this AlertRuleType ruleType)
    {
        return ruleType == AlertRuleType.PriceZone;
    }

    public static bool UsesRsi(this AlertRuleType ruleType)
    {
        return ruleType is AlertRuleType.RsiBelow
            or AlertRuleType.RsiAbove
            or AlertRuleType.RsiOversoldEmaCrossUp
            or AlertRuleType.RsiOverboughtEmaCrossDown;
    }

    public static bool UsesEma(this AlertRuleType ruleType)
    {
        return ruleType is AlertRuleType.EmaCrossUp
            or AlertRuleType.EmaCrossDown
            or AlertRuleType.RsiOversoldEmaCrossUp
            or AlertRuleType.RsiOverboughtEmaCrossDown;
    }

    public static bool UsesThreshold(this AlertRuleType ruleType)
    {
        return ruleType is not AlertRuleType.EmaCrossUp and not AlertRuleType.EmaCrossDown;
    }

    public static string DisplayName(this AlertRuleType ruleType)
    {
        return ruleType switch
        {
            AlertRuleType.PriceAbove => "TikTok short video",
            AlertRuleType.PriceBelow => "Instagram post/reel",
            AlertRuleType.PercentDrop24h => "Facebook campaign",
            AlertRuleType.PercentRise24h => "LinkedIn post",
            AlertRuleType.VolumeAbove24h => "Personalized email",
            AlertRuleType.PriceZone => "SMS promotion",
            AlertRuleType.RsiBelow => "Retargeting audience",
            AlertRuleType.RsiAbove => "Launch announcement",
            AlertRuleType.RsiOversoldEmaCrossUp => "Influencer brief",
            AlertRuleType.RsiOverboughtEmaCrossDown => "Lead nurture sequence",
            AlertRuleType.EmaCrossUp => "Multi-channel push",
            AlertRuleType.EmaCrossDown => "Win-back campaign",
            _ => ruleType.ToString()
        };
    }
}
