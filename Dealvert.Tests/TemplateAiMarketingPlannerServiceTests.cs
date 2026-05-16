using Dealvert.Services;

namespace Dealvert.Tests;

public class TemplateAiMarketingPlannerServiceTests
{
    [Fact]
    public void Generate_Mvp14_CreatesFocusedMvpCampaignPackage()
    {
        var service = new TemplateAiMarketingPlannerService();

        var draft = service.Generate(new AiMarketingPlanRequest(
            "Dealvert",
            "https://promovert.example.com",
            "AI campaign software",
            "B2B founders",
            "turn a website into a measurable campaign",
            "qualified leads",
            "clear and direct",
            ["LinkedIn", "Instagram"],
            new DateOnly(2026, 5, 14),
            new DateOnly(2026, 5, 27),
            "Mvp14",
            "ana@example.com\njoao@example.com",
            new AiAudienceLocation("World", null, null, null, null, null)));

        Assert.Equal(10, draft.Posts.Count);
        Assert.Equal(5, draft.Posts.Count(x => x.Platform == "LinkedIn"));
        Assert.Equal(5, draft.Posts.Count(x => x.Platform == "Instagram"));
        Assert.Equal(3, draft.Emails.Count);
        Assert.NotNull(draft.LandingPage);
        Assert.False(string.IsNullOrWhiteSpace(draft.BusinessDna));
        Assert.All(draft.Posts, post => Assert.InRange(post.ScheduledForUtc, new DateTime(2026, 5, 14), new DateTime(2026, 5, 27, 23, 59, 59)));
    }

    [Fact]
    public void Generate_ClipsGeneratedTextToPersistedFieldLimits()
    {
        var service = new TemplateAiMarketingPlannerService();
        var longAudience = "industrial SMBs, factories, production companies, metalworking businesses, industrial maintenance teams, operations directors, B2B sales teams, and managers who need to reduce repetitive manual tasks";
        var longValue = "automate commercial processes, opportunity follow-up, customer communication, and recurring campaigns without relying on daily manual work";

        var draft = service.Generate(new AiMarketingPlanRequest(
            "SBI Flow",
            "https://sbiflow.example.com",
            "Software to automate operations and marketing in industrial companies.",
            longAudience,
            longValue,
            "subscriptions and demo requests",
            "professional, direct, and results-oriented",
            ["TikTok", "Instagram", "Facebook", "LinkedIn", "X", "YouTube Shorts"],
            new DateOnly(2026, 5, 13),
            new DateOnly(2026, 6, 12),
            "Daily",
            "ana@example.com\njoao@example.com",
            new AiAudienceLocation("City", "Portugal", "Lisbon", 38.7223, -9.1393, 35)));

        Assert.NotEmpty(draft.Posts);
        Assert.NotEmpty(draft.Emails);
        Assert.NotEmpty(draft.Leads);
        Assert.NotNull(draft.LandingPage);
        Assert.False(string.IsNullOrWhiteSpace(draft.BusinessDna));
        Assert.True(draft.BusinessDna.Length <= 1000);
        Assert.True(draft.LandingPage.Headline.Length <= 180);
        Assert.True(draft.LandingPage.Subheadline.Length <= 260);
        Assert.True(draft.LandingPage.Body.Length <= 1600);
        Assert.True(draft.LandingPage.PrimaryCallToAction.Length <= 120);

        Assert.All(draft.Posts, post =>
        {
            Assert.True(post.Platform.Length <= 40);
            Assert.True(post.Title.Length <= 140);
            Assert.True(post.Hook.Length <= 300);
            Assert.True(post.Caption.Length <= 1600);
            Assert.True(post.CreativeBrief.Length <= 900);
            Assert.True(post.Hashtags is null || post.Hashtags.Length <= 300);
            Assert.True(post.CallToAction.Length <= 180);
        });

        Assert.All(draft.Emails, email =>
        {
            Assert.True(email.Subject.Length <= 160);
            Assert.True(email.PreviewText.Length <= 220);
            Assert.True(email.Body.Length <= 4000);
            Assert.True(email.AudienceSegment.Length <= 160);
        });

        Assert.All(draft.Leads, lead =>
        {
            Assert.True(lead.CompanyProfile.Length <= 160);
            Assert.True(lead.Industry.Length <= 120);
            Assert.True(lead.ContactRole.Length <= 120);
            Assert.True(lead.EmailSearchHint.Length <= 180);
            Assert.True(lead.Reason.Length <= 500);
            Assert.Contains("Lisbon", lead.Reason);
        });
    }
}
