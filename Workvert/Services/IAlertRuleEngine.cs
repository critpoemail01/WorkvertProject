using Workvert.Models;

namespace Workvert.Services;

public interface IAlertRuleEngine
{
    RuleResult Evaluate(Alert alert, MarketSnapshot snapshot, TechnicalIndicatorSnapshot? technical = null);
}
