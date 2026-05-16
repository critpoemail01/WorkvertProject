using Dealvert.Models;

namespace Dealvert.Services;

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
            AlertRuleType.PriceBelowMargin => EvalPriceBelowMargin(alert, snapshot),
            AlertRuleType.DailyOpportunityReport => EvalDailyOpportunityReport(alert, snapshot),
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
        var msg = $"{s.Symbol}: estimated resale value EUR {s.ResaleBenchmarkPrice ?? s.Price:0.##} reached target EUR {alert.Threshold:0.##}";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalPriceBelow(Alert alert, MarketSnapshot s)
    {
        var hit = s.Price <= alert.Threshold;
        var msg = $"{s.Symbol}: found for EUR {s.Price:0.##} at {StoreLabel(s)}, within target EUR {alert.Threshold:0.##}";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalPercentDrop24h(Alert alert, MarketSnapshot s)
    {
        var hit = s.PercentChange24h <= alert.Threshold;
        var msg = $"{s.Symbol}: price drop {s.PercentChange24h:0.##}% reached limit {alert.Threshold:0.##}%";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalPercentRise24h(Alert alert, MarketSnapshot s)
    {
        var hit = s.PercentChange24h >= alert.Threshold;
        var msg = $"{s.Symbol}: estimated margin {s.OpportunityMarginPercent ?? s.PercentChange24h:0.##}% reached target {alert.Threshold:0.##}%";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalVolumeAbove24h(Alert alert, MarketSnapshot s)
    {
        if (s.Volume24h is null)
            return new RuleResult(false, $"{s.Symbol}: opportunity score unavailable");

        var hit = s.Volume24h.Value >= alert.Threshold;
        var msg = $"{s.Symbol}: opportunity score {s.Volume24h.Value:0.##} reached minimum {alert.Threshold:0.##}";
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
            return new RuleResult(false, $"{s.Symbol}: price EUR {s.Price:0.##} outside target zone EUR {min:0.##}-{max:0.##}");
        }

        if (alert.PriceZoneWasInside)
            return new RuleResult(false, $"{s.Symbol}: price EUR {s.Price:0.##} still inside target zone EUR {min:0.##}-{max:0.##}");

        alert.PriceZoneWasInside = true;
        var msg = $"{s.Symbol}: price EUR {s.Price:0.##} entered target zone EUR {min:0.##}-{max:0.##}";
        return new RuleResult(true, msg);
    }

    private static RuleResult EvalPriceBelowMargin(Alert alert, MarketSnapshot s)
    {
        var marginPercent = alert.ZonePercent <= 0 ? 1m : alert.ZonePercent;
        var requiredPrice = alert.Threshold * (1 - marginPercent / 100m);
        var hit = s.Price <= requiredPrice;
        var msg = hit
            ? $"{s.Symbol}: found for EUR {s.Price:0.##} at {StoreLabel(s)}, {marginPercent:0.##}% below target EUR {alert.Threshold:0.##}"
            : $"{s.Symbol}: EUR {s.Price:0.##} still above margin limit EUR {requiredPrice:0.##}";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalDailyOpportunityReport(Alert alert, MarketSnapshot s)
    {
        var score = s.Volume24h ?? 0m;
        var minimumScore = alert.Threshold <= 0 ? 20m : alert.Threshold;
        var hit = score >= minimumScore;
        var resale = s.ResaleBenchmarkPrice is null ? "no benchmark" : $"resale benchmark EUR {s.ResaleBenchmarkPrice.Value:0.##}";
        var margin = s.OpportunityMarginPercent is null ? "margin not calculated" : $"estimated margin {s.OpportunityMarginPercent.Value:0.##}%";
        var msg = $"{s.Category ?? "Category"}: {s.ProductName ?? s.Symbol} at {StoreLabel(s)} for EUR {s.Price:0.##}; {resale}; {margin}; score {score:0.##}";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalRsiBelow(Alert alert, TechnicalIndicatorSnapshot? t)
    {
        if (t is null)
            return new RuleResult(false, $"{alert.Symbol}: category cooling data unavailable ({alert.Timeframe})");

        alert.LastIndicatorValue = t.Rsi;
        var hit = t.Rsi <= alert.Threshold;
        var msg = $"{t.Symbol}: category cooling score {t.Rsi:0.##} met target {alert.Threshold:0.##}";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalRsiAbove(Alert alert, TechnicalIndicatorSnapshot? t)
    {
        if (t is null)
            return new RuleResult(false, $"{alert.Symbol}: category demand data unavailable ({alert.Timeframe})");

        alert.LastIndicatorValue = t.Rsi;
        var hit = t.Rsi >= alert.Threshold;
        var msg = $"{t.Symbol}: category demand score {t.Rsi:0.##} met target {alert.Threshold:0.##}";
        return new RuleResult(hit, msg);
    }

    private static RuleResult EvalRsiOversoldEmaCrossUp(Alert alert, TechnicalIndicatorSnapshot? t)
    {
        if (t is null)
            return new RuleResult(false, $"{alert.Symbol}: demand recovery data unavailable ({alert.Timeframe})");

        alert.LastIndicatorValue = t.Rsi;

        if (t.Rsi <= alert.Threshold)
            alert.IndicatorArmed = true;

        var crossedUp = t.FastEma > t.SlowEma &&
            (t.PreviousFastEma is null || t.PreviousSlowEma is null || t.PreviousFastEma <= t.PreviousSlowEma);

        if (!alert.IndicatorArmed || !crossedUp)
        {
            return new RuleResult(false, $"{t.Symbol}: waiting for demand recovery confirmation");
        }

        alert.IndicatorArmed = false;
        var msg = $"{t.Symbol}: demand recovery confirmed with {alert.FastEmaPeriod}/{alert.SlowEmaPeriod} trend sequence";
        return new RuleResult(true, msg);
    }

    private static RuleResult EvalRsiOverboughtEmaCrossDown(Alert alert, TechnicalIndicatorSnapshot? t)
    {
        if (t is null)
            return new RuleResult(false, $"{alert.Symbol}: demand slowdown data unavailable ({alert.Timeframe})");

        alert.LastIndicatorValue = t.Rsi;

        if (t.Rsi >= alert.Threshold)
            alert.IndicatorArmed = true;

        var crossedDown = t.FastEma < t.SlowEma &&
            (t.PreviousFastEma is null || t.PreviousSlowEma is null || t.PreviousFastEma >= t.PreviousSlowEma);

        if (!alert.IndicatorArmed || !crossedDown)
        {
            return new RuleResult(false, $"{t.Symbol}: waiting for demand slowdown confirmation");
        }

        alert.IndicatorArmed = false;
        var msg = $"{t.Symbol}: demand slowdown confirmed with {alert.FastEmaPeriod}/{alert.SlowEmaPeriod} trend sequence";
        return new RuleResult(true, msg);
    }

    private static RuleResult EvalEmaCrossUp(Alert alert, TechnicalIndicatorSnapshot? t)
    {
        if (t is null)
            return new RuleResult(false, $"{alert.Symbol}: trend data unavailable ({alert.Timeframe})");

        var crossedUp = t.FastEma > t.SlowEma &&
            (t.PreviousFastEma is null || t.PreviousSlowEma is null || t.PreviousFastEma <= t.PreviousSlowEma);

        var msg = crossedUp
            ? $"{t.Symbol}: product trend is rising with {alert.FastEmaPeriod}/{alert.SlowEmaPeriod} signal sequence"
            : $"{t.Symbol}: waiting for rising trend confirmation";
        return new RuleResult(crossedUp, msg);
    }

    private static RuleResult EvalEmaCrossDown(Alert alert, TechnicalIndicatorSnapshot? t)
    {
        if (t is null)
            return new RuleResult(false, $"{alert.Symbol}: trend data unavailable ({alert.Timeframe})");

        var crossedDown = t.FastEma < t.SlowEma &&
            (t.PreviousFastEma is null || t.PreviousSlowEma is null || t.PreviousFastEma >= t.PreviousSlowEma);

        var msg = crossedDown
            ? $"{t.Symbol}: product trend is falling with {alert.FastEmaPeriod}/{alert.SlowEmaPeriod} signal sequence"
            : $"{t.Symbol}: waiting for falling trend confirmation";
        return new RuleResult(crossedDown, msg);
    }

    private static string StoreLabel(MarketSnapshot snapshot)
    {
        return string.IsNullOrWhiteSpace(snapshot.StoreName) ? "verified store" : snapshot.StoreName;
    }
}
