using Alivert.Models;
using Alivert.Services;

namespace Alivert.Tests;

public class CampaignBusinessAnalyticsServiceTests
{
    [Fact]
    public void BuildCampaignReport_FocusesOnCapturedLeadsAndConversion()
    {
        var service = new CampaignBusinessAnalyticsService();
        var plan = BuildPlan("Demo campaign", "LinkedIn,Instagram");
        plan.Posts.Add(new MarketingPostSuggestion
        {
            Platform = "LinkedIn",
            Status = "Published",
            Title = "Book a demo",
            EstimatedReach = 100,
            EstimatedInteractions = 20,
            EstimatedConversions = 4
        });
        plan.Posts.Add(new MarketingPostSuggestion
        {
            Platform = "Instagram",
            Status = "Published",
            Title = "Carousel proof",
            EstimatedReach = 90,
            EstimatedInteractions = 12,
            EstimatedConversions = 1
        });
        plan.Emails.Add(new MarketingEmailSuggestion
        {
            Status = "Sent",
            Subject = "See the result",
            EstimatedReach = 50,
            EstimatedInteractions = 10,
            EstimatedConversions = 2
        });
        plan.LandingPage = new MarketingLandingPage
        {
            ViewCount = 30,
            Leads =
            [
                new MarketingLandingLead { Name = "Ana", Email = "ana@example.com", Source = "linkedin / social / demo" },
                new MarketingLandingLead { Name = "Joao", Email = "joao@example.com", Source = "email / email / demo" }
            ]
        };

        var report = service.BuildCampaignReport(plan);

        Assert.Equal(2, report.Leads);
        Assert.Equal(72, report.Clicks);
        Assert.Equal("LinkedIn", report.TopLeadChannel);
        Assert.Equal("1.00 credits/lead", report.CostPerLeadLabel);
        Assert.Contains("opens", report.BestEmailDetail);
        Assert.Contains("Repeat LinkedIn", report.RecommendedNextAction);
    }

    [Fact]
    public void BuildPortfolioReport_SelectsMostProfitableCampaign()
    {
        var service = new CampaignBusinessAnalyticsService();
        var stronger = BuildPlan("Strong campaign", "LinkedIn");
        stronger.Posts.Add(new MarketingPostSuggestion
        {
            Platform = "LinkedIn",
            Status = "Published",
            Title = "Proof post",
            EstimatedReach = 200,
            EstimatedInteractions = 40,
            EstimatedConversions = 10
        });
        stronger.LandingPage = new MarketingLandingPage
        {
            ViewCount = 60,
            Leads =
            [
                new MarketingLandingLead { Name = "Lead 1", Email = "lead1@example.com", Source = "linkedin / social / strong" },
                new MarketingLandingLead { Name = "Lead 2", Email = "lead2@example.com", Source = "linkedin / social / strong" }
            ]
        };

        var weaker = BuildPlan("Weak campaign", "LinkedIn,Instagram,Facebook");
        weaker.Posts.Add(new MarketingPostSuggestion
        {
            Platform = "Facebook",
            Status = "Published",
            Title = "Awareness post",
            EstimatedReach = 100,
            EstimatedInteractions = 10,
            EstimatedConversions = 1
        });
        weaker.LandingPage = new MarketingLandingPage
        {
            ViewCount = 20,
            Leads =
            [
                new MarketingLandingLead { Name = "Lead 3", Email = "lead3@example.com", Source = "facebook / social / weak" }
            ]
        };

        var report = service.BuildPortfolioReport([stronger, weaker]);

        Assert.Equal(3, report.Leads);
        Assert.Equal("Strong campaign", report.MostProfitableCampaign);
        Assert.Equal("LinkedIn", report.TopLeadChannel);
        Assert.Contains("credits/lead", report.CostPerLeadLabel);
    }

    private static MarketingPlan BuildPlan(string productName, string platforms)
    {
        return new MarketingPlan
        {
            Id = Random.Shared.Next(1, 10_000),
            UserId = "user-1",
            ProductName = productName,
            CompanyOrIdea = productName,
            TargetAudience = "B2B teams",
            ValueProposition = "Generate qualified leads",
            CampaignGoal = "Leads",
            Tone = "Clear",
            Platforms = platforms,
            BusinessDna = "Demo DNA"
        };
    }
}
