using Alivert.Models;

namespace Alivert.Services;

public interface IAlertDispatcher
{
    Task DispatchAsync(Alert alert, MarketSnapshot snapshot, string message, CancellationToken ct);
}
