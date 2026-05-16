using Workvert.Services;

namespace Workvert.Tests;

public class CampaignLibraryServiceTests
{
    [Fact]
    public void Recommend_ReturnsMixedSectorCampaignsForIndustrialSoftware()
    {
        var service = new CampaignLibraryService();

        var recommendations = service.Recommend(new CampaignLibraryRequest(
            "SBI Flow",
            "Industrial software for operations and production",
            "operations directors in factories",
            "replace Excel with dashboards and automation",
            "generate leads",
            "B2B SaaS / automation"));

        Assert.Contains(recommendations, x => x.Key == "industrial-efficiency");
        Assert.Contains(recommendations, x => x.Key == "b2b-demo");
        Assert.True(recommendations.Select(x => x.Sector).Distinct().Count() > 1);
    }

    [Fact]
    public void Recommend_ReturnsClinicCampaignsForHealthcareBrief()
    {
        var service = new CampaignLibraryService();

        var recommendations = service.Recommend(new CampaignLibraryRequest(
            "Good Life Clinic",
            "Dental clinic with online booking",
            "local patients",
            "fast consultation",
            "bookings",
            "Clinics and healthcare"));

        Assert.Contains(recommendations, x => x.Key == "clinics-first-visit");
        Assert.Contains(recommendations, x => x.Title.Contains("appointment", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Find_ReturnsTemplateByKey()
    {
        var service = new CampaignLibraryService();

        var template = service.Find("construction-quote");

        Assert.NotNull(template);
        Assert.Equal("Construction and renovation", template!.Sector);
        Assert.Contains("quote", template.Goal, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Recommend_ReturnsGeneralCampaignsWhenSectorIsUnknown()
    {
        var service = new CampaignLibraryService();

        var recommendations = service.Recommend(new CampaignLibraryRequest(
            "New Brand",
            "business idea without a defined sector yet",
            "potential customers",
            "clear proposition",
            "generate leads",
            null));

        Assert.All(recommendations, x => Assert.Equal("General growth", x.Sector));
        Assert.Contains(recommendations, x => x.Key == "general-lead-capture");
    }
}
