namespace Dealvert.Models;

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
        return ruleType is AlertRuleType.PriceZone or AlertRuleType.PriceBelowMargin;
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
            AlertRuleType.PriceAbove => "Resale above target",
            AlertRuleType.PriceBelow => "Price at or below target",
            AlertRuleType.PercentDrop24h => "Strong price drop",
            AlertRuleType.PercentRise24h => "Resale margin",
            AlertRuleType.VolumeAbove24h => "Opportunity score",
            AlertRuleType.PriceZone => "Inside price zone",
            AlertRuleType.PriceBelowMargin => "Margin below target price",
            AlertRuleType.DailyOpportunityReport => "Daily opportunity report",
            AlertRuleType.RsiBelow => "Category cooled down",
            AlertRuleType.RsiAbove => "Category heating up",
            AlertRuleType.RsiOversoldEmaCrossUp => "Demand recovery",
            AlertRuleType.RsiOverboughtEmaCrossDown => "Demand dropping",
            AlertRuleType.EmaCrossUp => "Trend rising",
            AlertRuleType.EmaCrossDown => "Trend falling",
            _ => ruleType.ToString()
        };
    }
}
