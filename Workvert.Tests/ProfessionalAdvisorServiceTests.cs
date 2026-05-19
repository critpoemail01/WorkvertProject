using Workvert.Services;

namespace Workvert.Tests;

public class ProfessionalAdvisorServiceTests
{
    [Fact]
    public void AnalyzeProfile_DeveloperProfile_RecommendsRelevantJobsAndSkills()
    {
        var service = new ProfessionalAdvisorService();

        var analysis = service.AnalyzeProfile(new ProfessionalProfileRequest(
            "Software developer",
            "5 years building internal applications and APIs for sales teams.",
            "C#, SQL Server, APIs, Git",
            "communication, autonomy",
            ".NET, Visual Studio",
            "Computer science degree",
            "Portuguese, English",
            "Portugal",
            "Remote",
            "Full-time job and freelance",
            "EUR 40k/year",
            "SaaS, automations and integrations",
            "https://example.com",
            "professional-photo.jpg"));

        Assert.Contains("Azure", analysis.SuggestedSkills);
        Assert.Contains("Docker", analysis.SuggestedSkills);
        Assert.NotEmpty(analysis.JobOpportunities);
        Assert.Contains(analysis.JobOpportunities, x => x.Title.Contains(".NET", StringComparison.OrdinalIgnoreCase));
        Assert.All(analysis.JobOpportunities, x => Assert.InRange(x.CompatibilityScore, 45, 100));
        Assert.Contains(analysis.JobOpportunities, x => x.MatchedSkills.Contains("C#", StringComparer.OrdinalIgnoreCase));
        Assert.NotEmpty(analysis.FreelanceOpportunities);
        Assert.Contains(analysis.FreelanceOpportunities, x => x.SuggestedAction.Contains("offer", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("photo", analysis.PhotoUsageNote, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AnalyzeProfile_ProfilePhoto_DoesNotChangeCompatibilityScore()
    {
        var service = new ProfessionalAdvisorService();
        var baseRequest = new ProfessionalProfileRequest(
            "Designer",
            "Branding and landing pages for small businesses.",
            "Figma, Photoshop, Illustrator, branding",
            "creativity",
            "Figma, Adobe Creative Cloud",
            "Design course",
            "Portuguese, English",
            "Portugal",
            "Remote",
            "Freelance",
            "EUR 35/h",
            "UI/UX and social media",
            null,
            null);

        var withoutPhoto = service.AnalyzeProfile(baseRequest);
        var withPhoto = service.AnalyzeProfile(baseRequest with { ProfilePhotoLabel = "avatar.png" });

        Assert.Equal(
            withoutPhoto.JobOpportunities.Select(x => x.CompatibilityScore),
            withPhoto.JobOpportunities.Select(x => x.CompatibilityScore));
        Assert.Equal(
            withoutPhoto.FreelanceOpportunities.Select(x => x.CompatibilityScore),
            withPhoto.FreelanceOpportunities.Select(x => x.CompatibilityScore));
    }

    [Fact]
    public void AnalyzeServiceRequest_WebsiteRequest_IdentifiesTechnologyAndWebProfessional()
    {
        var service = new ProfessionalAdvisorService();

        var analysis = service.AnalyzeServiceRequest(new ServiceRequestRequest(
            "I need someone to create a website for my company, with responsive design, SEO and a contact form.",
            "Portugal",
            "EUR 800-1500",
            "this month",
            true));

        Assert.Equal("Technology", analysis.ServiceArea);
        Assert.Contains("developer", analysis.ProfessionalType);
        Assert.Contains("seo", analysis.RequiredSkills, StringComparer.OrdinalIgnoreCase);
        Assert.NotEmpty(analysis.RecommendedProfessionals);
        Assert.Contains(analysis.RecommendedProfessionals, x => x.ProfessionalArea.Contains("web", StringComparison.OrdinalIgnoreCase));
        Assert.All(analysis.RecommendedProfessionals, x => Assert.NotEmpty(x.MatchReasons));
    }
}
