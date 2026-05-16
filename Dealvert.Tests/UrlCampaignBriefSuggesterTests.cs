using Dealvert.Services;

namespace Dealvert.Tests;

public class UrlCampaignBriefSuggesterTests
{
    [Fact]
    public void SuggestFromPage_DetectsApplicationTypeAndBuildsEditableBrief()
    {
        var metadata = new UrlCampaignBriefSuggester.PageMetadata(
            "Acme Growth CRM - Campaign automation",
            "AI campaign software for growth teams, email outreach, social posts and conversion tracking.",
            "",
            "",
            "");

        var suggestion = UrlCampaignBriefSuggester.SuggestFromPage(new Uri("https://acme.example.com"), metadata);

        Assert.Equal("Acme Growth CRM", suggestion.ProductName);
        Assert.Equal("Marketing / growth tool", suggestion.DetectedApplicationType);
        Assert.Contains("founders", suggestion.TargetAudience, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("campaign", suggestion.ValueProposition, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LinkedIn", suggestion.Platforms);
    }

    [Fact]
    public void ExtractMetadata_ReadsTitleAndMetaDescription()
    {
        const string html = """
            <html>
              <head>
                <title>SBI Flow | Industrial automation</title>
                <meta content="Automate sales and operations workflows for industrial SMEs." name="description">
              </head>
            </html>
            """;

        var metadata = UrlCampaignBriefSuggester.ExtractMetadata(html);

        Assert.Equal("SBI Flow | Industrial automation", metadata.Title);
        Assert.Equal("Automate sales and operations workflows for industrial SMEs.", metadata.Description);
    }

    [Fact]
    public void SuggestFromPage_DetectsIndustrialCompanyWithoutForcingSoftware()
    {
        var metadata = new UrlCampaignBriefSuggester.PageMetadata(
            "SBI Flow | Industrial operations",
            "Planning, production, maintenance and energy control for industrial SMEs.",
            "",
            "",
            "");

        var suggestion = UrlCampaignBriefSuggester.SuggestFromPage(new Uri("https://sbiflow.example.com"), metadata);

        Assert.Equal("Industrial / manufacturing", suggestion.DetectedApplicationType);
        Assert.Contains("industrial", suggestion.TargetAudience, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("diagnostic", suggestion.CampaignGoal, StringComparison.OrdinalIgnoreCase);
    }
}
