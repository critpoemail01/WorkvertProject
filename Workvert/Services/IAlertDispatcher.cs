using Workvert.Models;

namespace Workvert.Services;

public interface IAlertDispatcher
{
    Task DispatchAsync(Alert alert, MarketSnapshot snapshot, string message, CancellationToken ct);
}
