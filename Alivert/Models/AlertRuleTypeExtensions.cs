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
            AlertRuleType.PriceAbove => "Price above",
            AlertRuleType.PriceBelow => "Price below",
            AlertRuleType.PercentDrop24h => "24h percent drop",
            AlertRuleType.PercentRise24h => "24h percent rise",
            AlertRuleType.VolumeAbove24h => "24h volume above",
            AlertRuleType.PriceZone => "Price zone",
            AlertRuleType.RsiBelow => "RSI below",
            AlertRuleType.RsiAbove => "RSI above",
            AlertRuleType.RsiOversoldEmaCrossUp => "RSI oversold then EMA cross up",
            AlertRuleType.RsiOverboughtEmaCrossDown => "RSI overbought then EMA cross down",
            AlertRuleType.EmaCrossUp => "EMA cross up",
            AlertRuleType.EmaCrossDown => "EMA cross down",
            _ => ruleType.ToString()
        };
    }
}
