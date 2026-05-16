using Workvert.Data;
using Workvert.Models;
using Workvert.Services;
using Microsoft.EntityFrameworkCore;

namespace Workvert.Tests;

public class CompanyCampaignLearningServiceTests
{
    [Fact]
    public async Task BuildAsync_LearnsWinningPatternsForSameCompany()
    {
        await using var db = CreateDbContext();
        var plan = BuildPlan("user-1", "https://acme.example.com", "Acme Flow");
        plan.Posts.Add(new MarketingPostSuggestion
        {
            Platform = "LinkedIn",
            Status = "Published",
            ScheduledForUtc = DateTime.UtcNow.AddDays(-5),
            Title = "Case study: operations team wins time",
            Hook = "A customer result",
            Caption = "Proof and result for B2B buyers",
            CreativeBrief = "Case study with measurable result",
            CallToAction = "Book a demo",
            EstimatedReach = 100,
            EstimatedInteractions = 20,
            EstimatedConversions = 4
        });
        plan.Emails.Add(new MarketingEmailSuggestion
        {
            Status = "Sent",
            ScheduledForUtc = DateTime.UtcNow.AddDays(-4),
            Subject = "Quick result",
            PreviewText = "Short follow-up",
            Body = "Hi {FirstName}, book a demo?",
            AudienceSegment = "Operations leaders",
            EstimatedReach = 50,
            EstimatedInteractions = 12,
            EstimatedConversions = 2
        });
        plan.LandingPage = new MarketingLandingPage
        {
            Slug = "acme-flow",
            Headline = "Acme Flow",
            Subheadline = "Book a demo",
            Body = "Landing page",
            PrimaryCallToAction = "Book a demo",
            FormTitle = "Talk to us",
            FormIntro = "Simple form",
            ThankYouMessage = "Thanks",
            Status = "Published",
            ViewCount = 20,
            Leads =
            [
                new MarketingLandingLead
                {
                    Name = "Ana",
                    Email = "ana@example.com",
                    Source = "linkedin / social / acme",
                    CreatedAtUtc = new DateTime(2026, 5, 12, 9, 0, 0, DateTimeKind.Utc)
                }
            ]
        };
        db.MarketingPlans.Add(plan);
        db.MarketingPlans.Add(BuildPlan("other-user", "https://acme.example.com", "Acme Flow"));
        db.MarketingPlans.Add(BuildPlan("user-1", "https://other.example.com", "Other Flow"));
        await db.SaveChangesAsync();

        var service = new CompanyCampaignLearningService(db, new CampaignBusinessAnalyticsService());
        var profile = await service.BuildAsync("user-1", "Acme Flow", "https://acme.example.com", "Acme operations");

        Assert.True(profile.HasData);
        Assert.Equal(1, profile.CampaignsAnalyzed);
        Assert.Equal(1, profile.LeadsGenerated);
        Assert.Contains("LinkedIn", profile.PreferredPlatforms);
        Assert.Equal("case study posts", profile.PreferredPostStyle);
        Assert.Equal("Book a demo", profile.PreferredCta);
        Assert.Equal("short emails", profile.EmailStyle);
        Assert.Contains("Based on previous campaigns", profile.RecommendedCampaignBrief);
    }

    [Fact]
    public void Generate_AdaptsNextPlanFromCompanyLearning()
    {
        var service = new TemplateAiMarketingPlannerService();
        var learning = new CompanyLearningProfile(
            true,
            "acme.example.com",
            2,
            8,
            "This company converts with case studies.",
            ["case study posts performed best", "CTA: Book a demo"],
            ["Use Book a demo", "Prioritize LinkedIn"],
            ["LinkedIn"],
            "case study posts",
            "Book a demo",
            "short emails",
            "landing pages with a simple form",
            "Based on previous campaigns, recommend a case study campaign on LinkedIn with short emails follow-up.");

        var draft = service.Generate(new AiMarketingPlanRequest(
            "Acme Flow",
            "https://acme.example.com",
            "Workflow software",
            "Operations leaders",
            "reduce manual work",
            "qualified demos",
            "clear",
            ["Instagram", "LinkedIn"],
            new DateOnly(2026, 5, 14),
            new DateOnly(2026, 5, 16),
            "Daily",
            "ana@example.com",
            new AiAudienceLocation("World", null, null, null, null, null),
            learning));

        Assert.Equal("LinkedIn", draft.Posts.First().Platform);
        Assert.All(draft.Posts, post => Assert.Equal("Book a demo", post.CallToAction));
        Assert.Contains("Learning:", draft.BusinessDna);
        Assert.Equal("Book a demo", draft.LandingPage.PrimaryCallToAction);
        Assert.Contains("Short follow-up", draft.Emails.First().PreviewText);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static MarketingPlan BuildPlan(string userId, string productUrl, string productName)
    {
        return new MarketingPlan
        {
            UserId = userId,
            ProductName = productName,
            ProductUrl = productUrl,
            CompanyOrIdea = "Operations workflow software",
            TargetAudience = "Operations leaders",
            ValueProposition = "reduce manual work",
            CampaignGoal = "qualified demos",
            Tone = "clear",
            Platforms = "LinkedIn,Instagram",
            Frequency = "Daily",
            StartDate = new DateOnly(2026, 5, 1),
            EndDate = new DateOnly(2026, 5, 31),
            Status = "Published",
            BusinessDna = "Acme DNA"
        };
    }
}
