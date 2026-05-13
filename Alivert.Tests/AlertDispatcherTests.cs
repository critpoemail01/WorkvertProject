using Alivert.Data;
using Alivert.Models;
using Alivert.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Alivert.Tests;

public class AlertDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_RecordsTriggerAndSkippedDelivery_WhenEmailTransportIsNotConfigured()
    {
        await using var db = CreateDbContext();
        var alert = new Alert
        {
            UserId = "user-1",
            Symbol = "BTCUSDT",
            RuleType = AlertRuleType.PriceAbove,
            Threshold = 100,
            Channel = "Email",
            IsEnabled = true
        };
        db.Alerts.Add(alert);
        await db.SaveChangesAsync();

        var dispatcher = new AlertDispatcher(
            db,
            new TestHttpClientFactory(),
            new TestOptionsMonitor<NotificationOptions>(new NotificationOptions()),
            NullLogger<AlertDispatcher>.Instance);

        await dispatcher.DispatchAsync(
            alert,
            new MarketSnapshot(alert.Symbol, 110, 2, DateTime.UtcNow, 1_000_000),
            "BTCUSDT: price 110 >= 100",
            CancellationToken.None);
        await db.SaveChangesAsync();

        var trigger = await db.AlertTriggers.SingleAsync();
        var delivery = await db.AlertDeliveryLogs.SingleAsync();

        Assert.Equal(alert.Id, trigger.AlertId);
        Assert.Equal(trigger.Id, delivery.AlertTriggerId);
        Assert.Equal("Skipped", delivery.Status);
        Assert.Equal("Email", delivery.Channel);
        Assert.Contains("Email transport", delivery.ErrorMessage);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public TestOptionsMonitor(T value) => CurrentValue = value;

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
