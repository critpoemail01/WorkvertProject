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
        var msg = $"{s.Symbol}: TikTok reach signal {s.Price:0.##} met goal {alert.Threshold:0.##}";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalPriceBelow(Alert alert, MarketSnapshot s)
    {
        var hit = s.Price <= alert.Threshold;
        var msg = $"{s.Symbol}: Instagram engagement signal {s.Price:0.##} met goal {alert.Threshold:0.##}";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalPercentDrop24h(Alert alert, MarketSnapshot s)
    {
        var hit = s.PercentChange24h <= alert.Threshold;
        var msg = $"{s.Symbol}: Facebook audience signal {s.PercentChange24h:0.##}% met goal {alert.Threshold:0.##}%";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalPercentRise24h(Alert alert, MarketSnapshot s)
    {
        var hit = s.PercentChange24h >= alert.Threshold;
        var msg = $"{s.Symbol}: LinkedIn lead signal {s.PercentChange24h:0.##}% met goal {alert.Threshold:0.##}%";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalVolumeAbove24h(Alert alert, MarketSnapshot s)
    {
        if (s.Volume24h is null)
            return new RuleResult(false, $"{s.Symbol}: email audience data is unavailable");

        var hit = s.Volume24h.Value >= alert.Threshold;
        var msg = $"{s.Symbol}: email audience {s.Volume24h.Value:0.##} met goal {alert.Threshold:0.##}";
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
            return new RuleResult(false, $"{s.Symbol}: SMS offer signal {s.Price:0.####} outside target band {min:0.####}-{max:0.####}");
        }

        if (alert.PriceZoneWasInside)
            return new RuleResult(false, $"{s.Symbol}: SMS offer signal {s.Price:0.####} still inside target band {min:0.####}-{max:0.####}");

        alert.PriceZoneWasInside = true;
        var msg = $"{s.Symbol}: SMS offer signal {s.Price:0.####} entered target band {min:0.####}-{max:0.####}";
        return new RuleResult(true, msg);
    }

    private static RuleResult EvalRsiBelow(Alert alert, TechnicalIndicatorSnapshot? t)
    {
        if (t is null)
            return new RuleResult(false, $"{alert.Symbol}: retargeting audience data unavailable ({alert.Timeframe})");

        alert.LastIndicatorValue = t.Rsi;
        var hit = t.Rsi <= alert.Threshold;
        var msg = $"{t.Symbol}: retargeting audience score {t.Rsi:0.##} met goal {alert.Threshold:0.##}";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalRsiAbove(Alert alert, TechnicalIndicatorSnapshot? t)
    {
        if (t is null)
            return new RuleResult(false, $"{alert.Symbol}: launch audience data unavailable ({alert.Timeframe})");

        alert.LastIndicatorValue = t.Rsi;
        var hit = t.Rsi >= alert.Threshold;
        var msg = $"{t.Symbol}: launch audience score {t.Rsi:0.##} met goal {alert.Threshold:0.##}";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalRsiOversoldEmaCrossUp(Alert alert, TechnicalIndicatorSnapshot? t)
    {
        if (t is null)
            return new RuleResult(false, $"{alert.Symbol}: influencer audience data unavailable ({alert.Timeframe})");

        alert.LastIndicatorValue = t.Rsi;

        if (t.Rsi <= alert.Threshold)
            alert.IndicatorArmed = true;

        var crossedUp = t.FastEma > t.SlowEma &&
            (t.PreviousFastEma is null || t.PreviousSlowEma is null || t.PreviousFastEma <= t.PreviousSlowEma);

        if (!alert.IndicatorArmed || !crossedUp)
        {
            return new RuleResult(false, $"{t.Symbol}: waiting for creator audience readiness and content variants");
        }

        alert.IndicatorArmed = false;
        var msg = $"{t.Symbol}: influencer brief is ready for {alert.FastEmaPeriod}/{alert.SlowEmaPeriod} content sequence";
        return new RuleResult(true, msg);
    }

    private static RuleResult EvalRsiOverboughtEmaCrossDown(Alert alert, TechnicalIndicatorSnapshot? t)
    {
        if (t is null)
            return new RuleResult(false, $"{alert.Symbol}: nurture sequence data unavailable ({alert.Timeframe})");

        alert.LastIndicatorValue = t.Rsi;

        if (t.Rsi >= alert.Threshold)
            alert.IndicatorArmed = true;

        var crossedDown = t.FastEma < t.SlowEma &&
            (t.PreviousFastEma is null || t.PreviousSlowEma is null || t.PreviousFastEma >= t.PreviousSlowEma);

        if (!alert.IndicatorArmed || !crossedDown)
        {
            return new RuleResult(false, $"{t.Symbol}: waiting for lead nurture readiness and follow-up steps");
        }

        alert.IndicatorArmed = false;
        var msg = $"{t.Symbol}: lead nurture sequence is ready for {alert.FastEmaPeriod}/{alert.SlowEmaPeriod} follow-up steps";
        return new RuleResult(true, msg);
    }

    private static RuleResult EvalEmaCrossUp(Alert alert, TechnicalIndicatorSnapshot? t)
    {
        if (t is null)
            return new RuleResult(false, $"{alert.Symbol}: multi-channel data unavailable ({alert.Timeframe})");

        var crossedUp = t.FastEma > t.SlowEma &&
            (t.PreviousFastEma is null || t.PreviousSlowEma is null || t.PreviousFastEma <= t.PreviousSlowEma);

        var msg = crossedUp
            ? $"{t.Symbol}: multi-channel push is ready with {alert.FastEmaPeriod}/{alert.SlowEmaPeriod} touch sequence"
            : $"{t.Symbol}: waiting for multi-channel push readiness";
        return new RuleResult(crossedUp, msg);
    }

    private static RuleResult EvalEmaCrossDown(Alert alert, TechnicalIndicatorSnapshot? t)
    {
        if (t is null)
            return new RuleResult(false, $"{alert.Symbol}: win-back data unavailable ({alert.Timeframe})");

        var crossedDown = t.FastEma < t.SlowEma &&
            (t.PreviousFastEma is null || t.PreviousSlowEma is null || t.PreviousFastEma >= t.PreviousSlowEma);

        var msg = crossedDown
            ? $"{t.Symbol}: win-back campaign is ready with {alert.FastEmaPeriod}/{alert.SlowEmaPeriod} touch sequence"
            : $"{t.Symbol}: waiting for win-back readiness";
        return new RuleResult(crossedDown, msg);
    }
}
