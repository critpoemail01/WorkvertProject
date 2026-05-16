using Dealvert.Models;

namespace Dealvert.Services;

public interface IAlertDispatcher
{
    Task DispatchAsync(Alert alert, MarketSnapshot snapshot, string message, CancellationToken ct);
}
