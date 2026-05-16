using Dealvert.Data;
using Dealvert.Models;
using Dealvert.Services;
using Microsoft.EntityFrameworkCore;

namespace Dealvert.Workers;

public sealed class AlertEvaluatorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMarketDataService _market;
    private readonly ITechnicalIndicatorService _technicalIndicators;
    private readonly IAlertRuleEngine _ruleEngine;
    private readonly ILogger<AlertEvaluatorWorker> _logger;

    public AlertEvaluatorWorker(
        IServiceScopeFactory scopeFactory,
        IMarketDataService market,
        ITechnicalIndicatorService technicalIndicators,
        IAlertRuleEngine ruleEngine,
        ILogger<AlertEvaluatorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _market = market;
        _technicalIndicators = technicalIndicators;
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
                var accounts = scope.ServiceProvider.GetRequiredService<IUserAccountService>();

                var activeUserIds = await db.Alerts
                    .Where(a => a.IsEnabled)
                    .Select(a => a.UserId)
                    .Distinct()
                    .ToListAsync(stoppingToken);

                foreach (var userId in activeUserIds)
                {
                    var disabled = await accounts.EnforceActiveAlertLimitAsync(userId, stoppingToken);
                    if (disabled > 0)
                    {
                        _logger.LogInformation("Disabled {Count} alert(s) for user {UserId} after credit capacity expired.", disabled, userId);
                    }
                }

                var alerts = await db.Alerts
                    .Where(a => a.IsEnabled)
                    .ToListAsync(stoppingToken);

                var now = DateTime.UtcNow;
                var technicalCache = new Dictionary<string, TechnicalIndicatorSnapshot?>(StringComparer.OrdinalIgnoreCase);

                foreach (var group in alerts.GroupBy(a => a.Symbol))
                {
                    var snapshotCache = new Dictionary<string, MarketSnapshot>(StringComparer.OrdinalIgnoreCase);

                    foreach (var alert in group)
                    {
                        var snapshotKey = $"{alert.MarketType}|{alert.AudienceList}";
                        if (!snapshotCache.TryGetValue(snapshotKey, out var snapshot))
                        {
                            try
                            {
                                snapshot = await _market.GetSnapshotAsync(group.Key, alert.MarketType, stoppingToken, alert);
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (Exception ex)
                            {
                                if (ex is InvalidOperationException &&
                                    ex.Message.Contains("was not found", StringComparison.Ordinal))
                                {
                                    alert.IsEnabled = false;
                                    alert.UpdatedAtUtc = now;
                                    _logger.LogWarning(ex, "Disabled alert {AlertId}: {MarketType} symbol {Symbol} was not found.", alert.Id, alert.MarketType, alert.Symbol);
                                }
                                else
                                {
                                    _logger.LogWarning(ex, "Skipping alert {AlertId}: could not load {MarketType} market data for {Symbol}.", alert.Id, alert.MarketType, alert.Symbol);
                                }

                                continue;
                            }

                            snapshotCache[snapshotKey] = snapshot;
                        }

                        var isCoolingDown = alert.LastTriggeredAtUtc is not null &&
                            now - alert.LastTriggeredAtUtc.Value < TimeSpan.FromMinutes(alert.CooldownMinutes);

                        TechnicalIndicatorSnapshot? technical = null;
                        if (alert.RuleType.RequiresTechnicalIndicators())
                        {
                            var cacheKey = $"{alert.MarketType}|{alert.Symbol}|{alert.Timeframe}|{alert.RsiPeriod}|{alert.FastEmaPeriod}|{alert.SlowEmaPeriod}";
                            if (!technicalCache.TryGetValue(cacheKey, out technical))
                            {
                                technical = await _technicalIndicators.GetSnapshotAsync(
                                    alert.Symbol,
                                    alert.MarketType,
                                    alert.Timeframe,
                                    alert.RsiPeriod,
                                    alert.FastEmaPeriod,
                                    alert.SlowEmaPeriod,
                                    stoppingToken);
                                technicalCache[cacheKey] = technical;
                            }
                        }

                        alert.LastEvaluatedAtUtc = now;
                        var result = _ruleEngine.Evaluate(alert, snapshot, technical);
                        if (!result.Triggered) continue;
                        if (isCoolingDown) continue;

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
