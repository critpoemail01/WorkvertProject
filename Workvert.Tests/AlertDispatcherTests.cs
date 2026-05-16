using Workvert.Data;
using Workvert.Models;
using Workvert.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;

namespace Workvert.Tests;

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

    [Fact]
    public async Task DispatchAsync_SendsSlackDelivery_WhenSlackWebhookIsConfigured()
    {
        await using var db = CreateDbContext();
        var alert = new Alert
        {
            UserId = "user-1",
            Symbol = "BTCUSDT",
            RuleType = AlertRuleType.PriceAbove,
            Threshold = 100,
            Channel = "Slack",
            IsEnabled = true
        };
        db.Alerts.Add(alert);
        db.UserNotificationSettings.Add(new UserNotificationSettings
        {
            UserId = "user-1",
            SlackWebhookUrl = "https://hooks.slack.test/services/T000/B000/secret"
        });
        await db.SaveChangesAsync();

        HttpRequestMessage? request = null;
        string? requestBody = null;
        var dispatcher = new AlertDispatcher(
            db,
            new TestHttpClientFactory(async message =>
            {
                request = message;
                requestBody = await message.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK);
            }),
            new TestOptionsMonitor<NotificationOptions>(new NotificationOptions()),
            NullLogger<AlertDispatcher>.Instance);

        await dispatcher.DispatchAsync(
            alert,
            new MarketSnapshot(alert.Symbol, 110, 2, DateTime.UtcNow, 1_000_000),
            "BTCUSDT: price 110 >= 100",
            CancellationToken.None);
        await db.SaveChangesAsync();

        var delivery = await db.AlertDeliveryLogs.SingleAsync();

        Assert.Equal("Sent", delivery.Status);
        Assert.Equal("Slack", delivery.Channel);
        Assert.Equal("Slack webhook", delivery.Destination);
        Assert.Equal("https://hooks.slack.test/services/T000/B000/secret", request?.RequestUri?.ToString());
        Assert.Contains("Workvert price alert", requestBody);
        Assert.Contains("BTCUSDT", requestBody);
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
        private readonly HttpClient _client;

        public TestHttpClientFactory(Func<HttpRequestMessage, Task<HttpResponseMessage>>? responder = null)
        {
            _client = responder is null ? new HttpClient() : new HttpClient(new TestHandler(responder));
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class TestHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _responder;

        public TestHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _responder(request);
        }
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public TestOptionsMonitor(T value) => CurrentValue = value;

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
