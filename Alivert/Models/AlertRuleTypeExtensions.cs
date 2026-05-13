namespace Alivert.Models;

public static class AlertRuleTypeExtensions
{
    public static bool RequiresTechnicalIndicators(this AlertRuleType ruleType)
    {
        return ruleType is AlertRuleType.RsiBelow
            or AlertRuleType.RsiAbove
            or AlertRuleType.RsiOversoldEmaCrossUp
            or AlertRuleType.RsiOverboughtEmaCrossDown;
    }

    public static bool UsesPriceZone(this AlertRuleType ruleType)
    {
        return ruleType == AlertRuleType.PriceZone;
    }
}
