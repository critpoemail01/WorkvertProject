using Alivert.Models;

namespace Alivert.Services;

public sealed class AlertRuleEngine : IAlertRuleEngine
{
    public RuleResult Evaluate(Alert alert, MarketSnapshot snapshot)
    {
        return alert.RuleType switch
        {
            AlertRuleType.PriceAbove => EvalPriceAbove(alert, snapshot),
            AlertRuleType.PriceBelow => EvalPriceBelow(alert, snapshot),
            AlertRuleType.PercentDrop24h => EvalPercentDrop24h(alert, snapshot),
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
}
