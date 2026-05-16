using Dealvert.Models;

namespace Dealvert.Services;

public interface IAlertRuleEngine
{
    RuleResult Evaluate(Alert alert, MarketSnapshot snapshot, TechnicalIndicatorSnapshot? technical = null);
}
