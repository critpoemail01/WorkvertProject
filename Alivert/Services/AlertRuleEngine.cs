using Alivert.Models;

namespace Alivert.Services;

public sealed class AlertRuleEngine : IAlertRuleEngine
{
    public RuleResult Evaluate(Alert alert, MarketSnapshot snapshot, TechnicalIndicatorSnapshot? technical = null)
    {
        return alert.RuleType switch
        {
            AlertRuleType.PriceAbove => EvalPriceAbove(alert, snapshot),
            AlertRuleType.PriceBelow => EvalPriceBelow(alert, snapshot),
            AlertRuleType.PercentDrop24h => EvalPercentDrop24h(alert, snapshot),
            AlertRuleType.PercentRise24h => EvalPercentRise24h(alert, snapshot),
            AlertRuleType.VolumeAbove24h => EvalVolumeAbove24h(alert, snapshot),
            AlertRuleType.PriceZone => EvalPriceZone(alert, snapshot),
            AlertRuleType.RsiBelow => EvalRsiBelow(alert, technical),
            AlertRuleType.RsiAbove => EvalRsiAbove(alert, technical),
            AlertRuleType.RsiOversoldEmaCrossUp => EvalRsiOversoldEmaCrossUp(alert, technical),
            AlertRuleType.RsiOverboughtEmaCrossDown => EvalRsiOverboughtEmaCrossDown(alert, technical),
            AlertRuleType.EmaCrossUp => EvalEmaCrossUp(alert, technical),
            AlertRuleType.EmaCrossDown => EvalEmaCrossDown(alert, technical),
            _ => new RuleResult(false, "Unknown rule")
        };
    }

    private static RuleResult EvalPriceAbove(Alert alert, MarketSnapshot s)
    {
        var hit = s.Price >= alert.Threshold;
        var msg = $"{s.Symbol}: price {s.Price} >= {alert.Threshold} (PriceAbove)";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalPriceBelow(Alert alert, MarketSnapshot s)
    {
        var hit = s.Price <= alert.Threshold;
        var msg = $"{s.Symbol}: price {s.Price} <= {alert.Threshold} (PriceBelow)";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalPercentDrop24h(Alert alert, MarketSnapshot s)
    {
        // threshold is typically negative (e.g., -3)
        var hit = s.PercentChange24h <= alert.Threshold;
        var msg = $"{s.Symbol}: 24h change {s.PercentChange24h}% <= {alert.Threshold}% (PercentDrop24h)";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalPercentRise24h(Alert alert, MarketSnapshot s)
    {
        var hit = s.PercentChange24h >= alert.Threshold;
        var msg = $"{s.Symbol}: 24h change {s.PercentChange24h}% >= {alert.Threshold}% (PercentRise24h)";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalVolumeAbove24h(Alert alert, MarketSnapshot s)
    {
        if (s.Volume24h is null)
            return new RuleResult(false, $"{s.Symbol}: 24h volume is unavailable (VolumeAbove24h)");

        var hit = s.Volume24h.Value >= alert.Threshold;
        var msg = $"{s.Symbol}: 24h volume {s.Volume24h.Value} >= {alert.Threshold} (VolumeAbove24h)";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalPriceZone(Alert alert, MarketSnapshot s)
    {
        var zonePercent = alert.ZonePercent <= 0 ? 1.0m : alert.ZonePercent;
        var min = alert.Threshold * (1 - zonePercent / 100m);
        var max = alert.Threshold * (1 + zonePercent / 100m);
        var isInside = s.Price >= min && s.Price <= max;

        if (!isInside)
        {
            alert.PriceZoneWasInside = false;
            return new RuleResult(false, $"{s.Symbol}: price {s.Price:0.####} outside zone {min:0.####}-{max:0.####} around {alert.Threshold:0.####} (PriceZone)");
        }

        if (alert.PriceZoneWasInside)
            return new RuleResult(false, $"{s.Symbol}: price {s.Price:0.####} still inside zone {min:0.####}-{max:0.####} around {alert.Threshold:0.####} (PriceZone)");

        alert.PriceZoneWasInside = true;
        var msg = $"{s.Symbol}: price {s.Price:0.####} entered zone {min:0.####}-{max:0.####} around {alert.Threshold:0.####} (PriceZone)";
        return new RuleResult(true, msg);
    }

    private static RuleResult EvalRsiBelow(Alert alert, TechnicalIndicatorSnapshot? t)
    {
        if (t is null)
            return new RuleResult(false, $"{alert.Symbol}: RSI data unavailable ({alert.Timeframe})");

        alert.LastIndicatorValue = t.Rsi;
        var hit = t.Rsi <= alert.Threshold;
        var msg = $"{t.Symbol}: RSI({alert.RsiPeriod}) {t.Timeframe} {t.Rsi:0.##} <= {alert.Threshold:0.##} (RsiBelow)";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalRsiAbove(Alert alert, TechnicalIndicatorSnapshot? t)
    {
        if (t is null)
            return new RuleResult(false, $"{alert.Symbol}: RSI data unavailable ({alert.Timeframe})");

        alert.LastIndicatorValue = t.Rsi;
        var hit = t.Rsi >= alert.Threshold;
        var msg = $"{t.Symbol}: RSI({alert.RsiPeriod}) {t.Timeframe} {t.Rsi:0.##} >= {alert.Threshold:0.##} (RsiAbove)";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalRsiOversoldEmaCrossUp(Alert alert, TechnicalIndicatorSnapshot? t)
    {
        if (t is null)
            return new RuleResult(false, $"{alert.Symbol}: RSI/EMA data unavailable ({alert.Timeframe})");

        alert.LastIndicatorValue = t.Rsi;

        if (t.Rsi <= alert.Threshold)
            alert.IndicatorArmed = true;

        var crossedUp = t.FastEma > t.SlowEma &&
            (t.PreviousFastEma is null || t.PreviousSlowEma is null || t.PreviousFastEma <= t.PreviousSlowEma);

        if (!alert.IndicatorArmed || !crossedUp)
        {
            return new RuleResult(false, $"{t.Symbol}: waiting for oversold RSI then EMA {alert.FastEmaPeriod}/{alert.SlowEmaPeriod} cross up");
        }

        alert.IndicatorArmed = false;
        var msg = $"{t.Symbol}: [{t.Timeframe}] RSI({alert.RsiPeriod}) recovered from <= {alert.Threshold:0.##}; EMA{alert.FastEmaPeriod} crossed above EMA{alert.SlowEmaPeriod} (RsiOversoldEmaCrossUp)";
        return new RuleResult(true, msg);
    }

    private static RuleResult EvalRsiOverboughtEmaCrossDown(Alert alert, TechnicalIndicatorSnapshot? t)
    {
        if (t is null)
            return new RuleResult(false, $"{alert.Symbol}: RSI/EMA data unavailable ({alert.Timeframe})");

        alert.LastIndicatorValue = t.Rsi;

        if (t.Rsi >= alert.Threshold)
            alert.IndicatorArmed = true;

        var crossedDown = t.FastEma < t.SlowEma &&
            (t.PreviousFastEma is null || t.PreviousSlowEma is null || t.PreviousFastEma >= t.PreviousSlowEma);

        if (!alert.IndicatorArmed || !crossedDown)
        {
            return new RuleResult(false, $"{t.Symbol}: waiting for overbought RSI then EMA {alert.FastEmaPeriod}/{alert.SlowEmaPeriod} cross down");
        }

        alert.IndicatorArmed = false;
        var msg = $"{t.Symbol}: [{t.Timeframe}] RSI({alert.RsiPeriod}) pulled back from >= {alert.Threshold:0.##}; EMA{alert.FastEmaPeriod} crossed below EMA{alert.SlowEmaPeriod} (RsiOverboughtEmaCrossDown)";
        return new RuleResult(true, msg);
    }

    private static RuleResult EvalEmaCrossUp(Alert alert, TechnicalIndicatorSnapshot? t)
    {
        if (t is null)
            return new RuleResult(false, $"{alert.Symbol}: EMA data unavailable ({alert.Timeframe})");

        var crossedUp = t.FastEma > t.SlowEma &&
            (t.PreviousFastEma is null || t.PreviousSlowEma is null || t.PreviousFastEma <= t.PreviousSlowEma);

        var msg = crossedUp
            ? $"{t.Symbol}: [{t.Timeframe}] EMA{alert.FastEmaPeriod} crossed above EMA{alert.SlowEmaPeriod} (EmaCrossUp)"
            : $"{t.Symbol}: waiting for EMA{alert.FastEmaPeriod} to cross above EMA{alert.SlowEmaPeriod}";
        return new RuleResult(crossedUp, msg);
    }

    private static RuleResult EvalEmaCrossDown(Alert alert, TechnicalIndicatorSnapshot? t)
    {
        if (t is null)
            return new RuleResult(false, $"{alert.Symbol}: EMA data unavailable ({alert.Timeframe})");

        var crossedDown = t.FastEma < t.SlowEma &&
            (t.PreviousFastEma is null || t.PreviousSlowEma is null || t.PreviousFastEma >= t.PreviousSlowEma);

        var msg = crossedDown
            ? $"{t.Symbol}: [{t.Timeframe}] EMA{alert.FastEmaPeriod} crossed below EMA{alert.SlowEmaPeriod} (EmaCrossDown)"
            : $"{t.Symbol}: waiting for EMA{alert.FastEmaPeriod} to cross below EMA{alert.SlowEmaPeriod}";
        return new RuleResult(crossedDown, msg);
    }
}
