using System.Net;
using Alivert.Models;
using Alivert.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Alivert.Tests;

public class SymbolCatalogServiceTests
{
    [Fact]
    public async Task SearchAsync_ReturnsTradingBinanceSymbols()
    {
        var service = CreateService(request =>
        {
            Assert.Contains("/api/v3/exchangeInfo", request.RequestUri!.ToString());
            return Json("""
                {
                  "symbols": [
                    { "symbol": "AAVEBTC", "status": "TRADING", "baseAsset": "AAVE", "quoteAsset": "BTC" },
                    { "symbol": "BTCUSDT", "status": "TRADING", "baseAsset": "BTC", "quoteAsset": "USDT" },
                    { "symbol": "ETHARS", "status": "TRADING", "baseAsset": "ETH", "quoteAsset": "ARS" },
                    { "symbol": "ETHUSDT", "status": "TRADING", "baseAsset": "ETH", "quoteAsset": "USDT" },
                    { "symbol": "OLDUSDT", "status": "BREAK", "baseAsset": "OLD", "quoteAsset": "USDT" }
                  ]
                }
                """);
        });

        var results = await service.SearchAsync(MarketType.Crypto, "btc", CancellationToken.None);
        var valid = await service.IsValidSymbolAsync(MarketType.Crypto, "btcusdt", CancellationToken.None);

        Assert.Equal("BTCUSDT", results[0].Symbol);
        Assert.Contains(results, x => x.Symbol == "AAVEBTC");
        Assert.True(valid);

        var ethResults = await service.SearchAsync(MarketType.Crypto, "eth", CancellationToken.None);
        Assert.Equal("ETHUSDT", ethResults[0].Symbol);
    }

    [Fact]
    public async Task SearchAsync_ReturnsYahooSymbols()
    {
        var service = CreateService(request =>
        {
            Assert.Contains("/v1/finance/search", request.RequestUri!.ToString());
            return Json("""
                {
                  "quotes": [
                    { "symbol": "AAPL", "shortname": "Apple Inc.", "exchDisp": "NasdaqGS" },
                    { "symbol": "AAPL", "shortname": "Apple Inc.", "exchDisp": "NasdaqGS" }
                  ]
                }
                """);
        });

        var results = await service.SearchAsync(MarketType.Traditional, "apple", CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal("AAPL", result.Symbol);
        Assert.Equal("Apple Inc.", result.Name);
        Assert.Equal("NasdaqGS", result.Exchange);
    }

    [Fact]
    public async Task IsValidSymbolAsync_UsesYahooChartResult()
    {
        var service = CreateService(request =>
        {
            Assert.Contains("/v8/finance/chart/AAPL", request.RequestUri!.ToString());
            return Json("""
                { "chart": { "result": [ { "meta": { "symbol": "AAPL" } } ], "error": null } }
                """);
        });

        var valid = await service.IsValidSymbolAsync(MarketType.Traditional, "aapl", CancellationToken.None);

        Assert.True(valid);
    }

    private static SymbolCatalogService CreateService(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        return new SymbolCatalogService(
            new TestHttpClientFactory(responder),
            new TestOptionsMonitor<MarketDataOptions>(new MarketDataOptions
            {
                BinanceBaseUrl = "https://api.binance.com",
                RequestTimeoutSeconds = 5
            }),
            NullLogger<SymbolCatalogService>.Instance);
    }

    private static HttpResponseMessage Json(string content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content)
        };
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public TestHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public HttpClient CreateClient(string name) => new(new TestHandler(_responder));
    }

    private sealed class TestHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public TestHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
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
