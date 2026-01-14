using Alivert.Data;
using Alivert.Services;
using Microsoft.EntityFrameworkCore;

namespace Alivert.Workers;

public sealed class AlertEvaluatorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMarketDataService _market;
    private readonly IAlertRuleEngine _ruleEngine;
    private readonly ILogger<AlertEvaluatorWorker> _logger;

    public AlertEvaluatorWorker(IServiceScopeFactory scopeFactory, IMarketDataService market, IAlertRuleEngine ruleEngine, ILogger<AlertEvaluatorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _market = market;
        _ruleEngine = ruleEngine;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IAlertDispatcher>();

                var alerts = await db.Alerts
                    .AsNoTracking()
                    .Where(a => a.IsEnabled)
                    .ToListAsync(stoppingToken);

                var now = DateTime.UtcNow;

                foreach (var group in alerts.GroupBy(a => a.Symbol))
                {
                    var snapshot = await _market.GetSnapshotAsync(group.Key, stoppingToken);

                    foreach (var alert in group)
                    {
                        if (alert.LastTriggeredAtUtc is not null &&
                            now - alert.LastTriggeredAtUtc.Value < TimeSpan.FromMinutes(alert.CooldownMinutes))
                        {
                            continue;
                        }

                        var result = _ruleEngine.Evaluate(alert, snapshot);
                        if (!result.Triggered) continue;

                        db.Alerts.Attach(alert);
                        alert.LastTriggeredAtUtc = now;
                        alert.UpdatedAtUtc = now;
                        await dispatcher.DispatchAsync(alert, snapshot, result.Message, stoppingToken);
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while evaluating alerts.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
