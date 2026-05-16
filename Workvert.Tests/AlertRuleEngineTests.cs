using Workvert.Models;
using Workvert.Services;

namespace Workvert.Tests;

public class AlertRuleEngineTests
{
    private readonly AlertRuleEngine _engine = new();

    [Theory]
    [InlineData(AlertRuleType.PriceAbove, 100, 100, true)]
    [InlineData(AlertRuleType.PriceAbove, 100, 99.99, false)]
    [InlineData(AlertRuleType.PriceBelow, 100, 100, true)]
    [InlineData(AlertRuleType.PriceBelow, 100, 100.01, false)]
    public void PriceRules_EvaluateThresholds(AlertRuleType ruleType, decimal threshold, decimal price, bool expected)
    {
        var result = _engine.Evaluate(
            Alert(ruleType, threshold),
            new MarketSnapshot("BTCUSDT", price, 0, DateTime.UtcNow));

        Assert.Equal(expected, result.Triggered);
    }

    [Theory]
    [InlineData(AlertRuleType.PercentDrop24h, -3, -3, true)]
    [InlineData(AlertRuleType.PercentDrop24h, -3, -2.99, false)]
    [InlineData(AlertRuleType.PercentRise24h, 3, 3, true)]
    [InlineData(AlertRuleType.PercentRise24h, 3, 2.99, false)]
    public void PercentRules_EvaluateTwentyFourHourChange(AlertRuleType ruleType, decimal threshold, decimal percentChange, bool expected)
    {
        var result = _engine.Evaluate(
            Alert(ruleType, threshold),
            new MarketSnapshot("ETHUSDT", 2500, percentChange, DateTime.UtcNow));

        Assert.Equal(expected, result.Triggered);
    }

    [Fact]
    public void VolumeAbove24h_Triggers_WhenVolumeIsAtOrAboveThreshold()
    {
        var result = _engine.Evaluate(
            Alert(AlertRuleType.VolumeAbove24h, 1_000_000),
            new MarketSnapshot("SOLUSDT", 150, 2, DateTime.UtcNow, 1_000_000));

        Assert.True(result.Triggered);
    }

    [Fact]
    public void VolumeAbove24h_DoesNotTrigger_WhenVolumeIsUnavailable()
    {
        var result = _engine.Evaluate(
            Alert(AlertRuleType.VolumeAbove24h, 1_000_000),
            new MarketSnapshot("SOLUSDT", 150, 2, DateTime.UtcNow));

        Assert.False(result.Triggered);
        Assert.Contains("unavailable", result.Message);
    }

    [Fact]
    public void PriceZone_Triggers_WhenPriceIsInsideConfiguredPercentBand()
    {
        var alert = Alert(AlertRuleType.PriceZone, 100);
        alert.ZonePercent = 1;

        var result = _engine.Evaluate(
            alert,
            new MarketSnapshot("AAPL", 100.50m, 0, DateTime.UtcNow));

        Assert.True(result.Triggered);
        Assert.True(alert.PriceZoneWasInside);
        Assert.Contains("entered target zone", result.Message);
    }

    [Fact]
    public void PriceZone_DoesNotRepeat_WhenPriceStaysInsideConfiguredPercentBand()
    {
        var alert = Alert(AlertRuleType.PriceZone, 100);
        alert.ZonePercent = 1;

        var first = _engine.Evaluate(
            alert,
            new MarketSnapshot("AAPL", 100.50m, 0, DateTime.UtcNow));
        var second = _engine.Evaluate(
            alert,
            new MarketSnapshot("AAPL", 100.25m, 0, DateTime.UtcNow));

        Assert.True(first.Triggered);
        Assert.False(second.Triggered);
        Assert.True(alert.PriceZoneWasInside);
    }

    [Fact]
    public void PriceZone_Rearms_WhenPriceLeavesConfiguredPercentBand()
    {
        var alert = Alert(AlertRuleType.PriceZone, 100);
        alert.ZonePercent = 1;
        alert.PriceZoneWasInside = true;

        var result = _engine.Evaluate(
            alert,
            new MarketSnapshot("AAPL", 102m, 0, DateTime.UtcNow));

        Assert.False(result.Triggered);
        Assert.False(alert.PriceZoneWasInside);
    }

    [Fact]
    public void PriceZone_DoesNotTrigger_WhenPriceIsOutsideConfiguredPercentBand()
    {
        var alert = Alert(AlertRuleType.PriceZone, 100);
        alert.ZonePercent = 1;

        var result = _engine.Evaluate(
            alert,
            new MarketSnapshot("AAPL", 102m, 0, DateTime.UtcNow));

        Assert.False(result.Triggered);
    }

    [Fact]
    public void PriceBelowMargin_Triggers_WhenPriceIsBelowTargetByConfiguredMargin()
    {
        var alert = Alert(AlertRuleType.PriceBelowMargin, 100);
        alert.ZonePercent = 10;

        var result = _engine.Evaluate(
            alert,
            new MarketSnapshot("Console @ Amazon", 89.99m, 0, DateTime.UtcNow, StoreName: "Amazon"));

        Assert.True(result.Triggered);
    }

    [Fact]
    public void DailyOpportunityReport_Triggers_WhenOpportunityScoreMeetsMinimum()
    {
        var result = _engine.Evaluate(
            Alert(AlertRuleType.DailyOpportunityReport, 20),
            new MarketSnapshot("Camera @ Fnac", 199, 0, DateTime.UtcNow, 45, "Fnac", "Camera", "https://example.com", 260, 23, "Portugal", "Lisboa", "Fotografia"));

        Assert.True(result.Triggered);
        Assert.Contains("score 45", result.Message);
    }

    [Fact]
    public void RsiBelow_Triggers_WhenRsiIsBelowThreshold()
    {
        var result = _engine.Evaluate(
            Alert(AlertRuleType.RsiBelow, 30),
            new MarketSnapshot("BTCUSDT", 100, 0, DateTime.UtcNow),
            Technical(rsi: 29, fast: 98, slow: 100));

        Assert.True(result.Triggered);
    }

    [Fact]
    public void RsiAbove_Triggers_WhenRsiIsAboveThreshold()
    {
        var result = _engine.Evaluate(
            Alert(AlertRuleType.RsiAbove, 70),
            new MarketSnapshot("BTCUSDT", 100, 0, DateTime.UtcNow),
            Technical(rsi: 71, fast: 101, slow: 100));

        Assert.True(result.Triggered);
    }

    [Fact]
    public void EmaCrossUp_Triggers_WhenFastEmaCrossesAboveSlowEma()
    {
        var result = _engine.Evaluate(
            Alert(AlertRuleType.EmaCrossUp, 0),
            new MarketSnapshot("BTCUSDT", 100, 0, DateTime.UtcNow),
            Technical(rsi: 50, fast: 102, slow: 100, previousFast: 99, previousSlow: 100));

        Assert.True(result.Triggered);
    }

    [Fact]
    public void EmaCrossDown_Triggers_WhenFastEmaCrossesBelowSlowEma()
    {
        var result = _engine.Evaluate(
            Alert(AlertRuleType.EmaCrossDown, 0),
            new MarketSnapshot("BTCUSDT", 100, 0, DateTime.UtcNow),
            Technical(rsi: 50, fast: 98, slow: 100, previousFast: 101, previousSlow: 100));

        Assert.True(result.Triggered);
    }

    [Fact]
    public void RsiOversoldEmaCrossUp_ArmsOnOversold_ThenTriggersOnCrossUp()
    {
        var alert = Alert(AlertRuleType.RsiOversoldEmaCrossUp, 30);

        var armResult = _engine.Evaluate(
            alert,
            new MarketSnapshot("BTCUSDT", 100, 0, DateTime.UtcNow),
            Technical(rsi: 29, fast: 98, slow: 100, previousFast: 99, previousSlow: 100));

        Assert.False(armResult.Triggered);
        Assert.True(alert.IndicatorArmed);

        var triggerResult = _engine.Evaluate(
            alert,
            new MarketSnapshot("BTCUSDT", 103, 0, DateTime.UtcNow),
            Technical(rsi: 34, fast: 102, slow: 100, previousFast: 99, previousSlow: 100));

        Assert.True(triggerResult.Triggered);
        Assert.False(alert.IndicatorArmed);
    }

    [Fact]
    public void RsiOverboughtEmaCrossDown_ArmsOnOverbought_ThenTriggersOnCrossDown()
    {
        var alert = Alert(AlertRuleType.RsiOverboughtEmaCrossDown, 70);

        var armResult = _engine.Evaluate(
            alert,
            new MarketSnapshot("BTCUSDT", 100, 0, DateTime.UtcNow),
            Technical(rsi: 72, fast: 102, slow: 100, previousFast: 101, previousSlow: 100));

        Assert.False(armResult.Triggered);
        Assert.True(alert.IndicatorArmed);

        var triggerResult = _engine.Evaluate(
            alert,
            new MarketSnapshot("BTCUSDT", 97, 0, DateTime.UtcNow),
            Technical(rsi: 65, fast: 98, slow: 100, previousFast: 101, previousSlow: 100));

        Assert.True(triggerResult.Triggered);
        Assert.False(alert.IndicatorArmed);
    }

    private static Alert Alert(AlertRuleType ruleType, decimal threshold)
    {
        return new Alert
        {
            Id = 1,
            UserId = "user-1",
            Symbol = "BTCUSDT",
            RuleType = ruleType,
            Threshold = threshold,
            Timeframe = "4h",
            RsiPeriod = 14,
            FastEmaPeriod = 3,
            SlowEmaPeriod = 5,
            IsEnabled = true
        };
    }

    private static TechnicalIndicatorSnapshot Technical(
        decimal rsi,
        decimal fast,
        decimal slow,
        decimal? previousFast = null,
        decimal? previousSlow = null)
    {
        return new TechnicalIndicatorSnapshot(
            "BTCUSDT",
            "4h",
            rsi,
            fast,
            slow,
            previousFast,
            previousSlow,
            DateTime.UtcNow);
    }
}
