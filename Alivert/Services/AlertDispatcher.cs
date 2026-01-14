using Alivert.Data;
using Alivert.Models;
using System.Text.Json;

namespace Alivert.Services;

/// <summary>
/// MVP dispatcher: writes triggers to the database (in the current DbContext).
/// Later you can add Email/Telegram/Discord delivery here.
/// </summary>
public sealed class AlertDispatcher : IAlertDispatcher
{
    private readonly ApplicationDbContext _db;

    public AlertDispatcher(ApplicationDbContext db) => _db = db;

    public Task DispatchAsync(Alert alert, MarketSnapshot snapshot, string message, CancellationToken ct)
    {
        _db.AlertTriggers.Add(new AlertTrigger
        {
            AlertId = alert.Id,
            TriggeredAtUtc = DateTime.UtcNow,
            Message = message,
            SnapshotJson = JsonSerializer.Serialize(snapshot)
        });

        return Task.CompletedTask;
    }
}
