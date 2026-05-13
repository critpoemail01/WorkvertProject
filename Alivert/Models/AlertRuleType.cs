namespace Alivert.Models;

public enum AlertRuleType
{
    PriceAbove = 1,
    PriceBelow = 2,
    PercentDrop24h = 3,
    PercentRise24h = 4,
    VolumeAbove24h = 5,
    PriceZone = 6,
    RsiBelow = 7,
    RsiAbove = 8,
    RsiOversoldEmaCrossUp = 9,
    RsiOverboughtEmaCrossDown = 10
}
