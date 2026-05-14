using Alivert.Services;
using System.Net;

namespace Alivert.Tests;

public class LeadDiscoveryServiceTests
{
    [Fact]
    public async Task DiscoverAsync_BuildsSearchQueriesFromAudienceAndLocation()
    {
        var service = new LeadDiscoveryService(new HttpClient(new TestHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound))));

        var result = await service.DiscoverAsync(new LeadDiscoveryRequest(
            "PME industriais e fabricas",
            "demos",
            "Portugal",
            null));

        Assert.NotEmpty(result.SearchQueries);
        Assert.Contains(result.SearchQueries, query => query.Url.Contains("Portugal"));
        Assert.Contains(result.SearchQueries, query => query.Label.Contains("industrial", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DiscoverAsync_ExtractsPublicEmailsFromSuppliedCompanyWebsites()
    {
        var service = new LeadDiscoveryService(new HttpClient(new TestHandler(request =>
        {
            var path = request.RequestUri?.AbsolutePath ?? "/";
            var html = path.Equals("/contact", StringComparison.OrdinalIgnoreCase)
                ? "<html><a href=\"mailto:sales@example.com\">sales@example.com</a><span>no-reply@example.com</span></html>"
                : "<html><a href=\"/contact\">Contact</a><span>hello@example.com</span></html>";

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html)
            };
        })));

        var result = await service.DiscoverAsync(new LeadDiscoveryRequest(
            "B2B SaaS",
            "subscriptions",
            "Worldwide",
            "https://example.com"));

        Assert.Contains(result.Emails, email => email.Email == "hello@example.com");
        Assert.Contains(result.Emails, email => email.Email == "sales@example.com");
        Assert.DoesNotContain(result.Emails, email => email.Email == "no-reply@example.com");
    }

    [Fact]
    public async Task DiscoverAsync_SkipsInternalUrls()
    {
        var service = new LeadDiscoveryService(new HttpClient(new TestHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))));

        var result = await service.DiscoverAsync(new LeadDiscoveryRequest(
            "B2B SaaS",
            "subscriptions",
            "Worldwide",
            "http://localhost:5107"));

        Assert.Empty(result.Emails);
        Assert.Contains(result.Warnings, warning => warning.Contains("internal", StringComparison.OrdinalIgnoreCase));
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
}
