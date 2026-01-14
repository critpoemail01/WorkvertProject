using Alivert.Models;

namespace Alivert.Services;

public interface IAlertRuleEngine
{
    RuleResult Evaluate(Alert alert, MarketSnapshot snapshot);
}
