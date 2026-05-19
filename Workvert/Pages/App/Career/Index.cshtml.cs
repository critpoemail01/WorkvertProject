using Workvert.Data;
using Workvert.Models;
using Workvert.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Workvert.Pages.App.Career;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IProfessionalAdvisorService _advisor;
    private readonly UserManager<IdentityUser> _userManager;

    public IndexModel(
        ApplicationDbContext db,
        IProfessionalAdvisorService advisor,
        UserManager<IdentityUser> userManager)
    {
        _db = db;
        _advisor = advisor;
        _userManager = userManager;
    }

    [BindProperty]
    public ProfileInput Input { get; set; } = new();

    public ProfessionalProfileAnalysis? Analysis { get; private set; }
    public string? SavedMessage { get; private set; }
    public int ProfileCompletion { get; private set; }
    public int SavedJobMatches { get; private set; }
    public int SavedServiceOffers { get; private set; }
    public int SavedAssets { get; private set; }
    public DateTime? LastProfileUpdateUtc { get; private set; }
    public string ActivePath { get; private set; } = "job";
    public string PathTitle => ActivePath switch
    {
        "freelance" => "I want to offer goods and services",
        _ => "I want to find the right work"
    };
    public string PathDescription => ActivePath switch
    {
        "freelance" => "Use the same profile to turn skills, goods, tools, or services into sellable offers and proposals.",
        _ => "Use the same profile to match skills, experience, goals, and location to company work."
    };

    public async Task OnGetAsync(string? path)
    {
        ActivePath = NormalizePath(path);
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        Input = await CreateInputAsync(userId, ActivePath);
        await LoadSavedSummaryAsync(userId);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ActivePath = NormalizePath(Input.Path);
        var userId = _userManager.GetUserId(User) ?? string.Empty;

        if (!ModelState.IsValid)
        {
            await LoadSavedSummaryAsync(userId);
            return Page();
        }

        Analysis = _advisor.AnalyzeProfile(Input.ToRequest());
        await SaveAnalysisAsync(userId, Input, Analysis);
        SavedMessage = "Profile, skills, matches, service offers, growth plan and generated copy were saved.";
        await LoadSavedSummaryAsync(userId);
        return Page();
    }

    private async Task<ProfileInput> CreateInputAsync(string userId, string path)
    {
        var profile = await _db.ProfessionalProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (profile is not null)
        {
            return new ProfileInput
            {
                Path = path,
                CurrentProfession = profile.CurrentProfession,
                Experience = profile.ExperienceSummary ?? string.Empty,
                TechnicalSkills = profile.TechnicalSkills ?? string.Empty,
                SoftSkills = profile.SoftSkills ?? string.Empty,
                Tools = profile.Tools ?? string.Empty,
                Education = profile.Education ?? string.Empty,
                Languages = profile.Languages ?? string.Empty,
                DesiredLocation = profile.DesiredLocation ?? string.Empty,
                WorkMode = profile.WorkMode,
                EngagementType = path == "freelance" ? "Freelance" : profile.EngagementType,
                CompensationGoal = profile.CompensationGoal ?? string.Empty,
                InterestAreas = profile.InterestAreas ?? string.Empty,
                PortfolioUrl = profile.PortfolioUrl,
                ProfilePhotoLabel = profile.ProfilePhotoPath
            };
        }

        return new ProfileInput
        {
            Path = path,
            WorkMode = "Flexible",
            EngagementType = path == "freelance" ? "Freelance" : "Full-time job"
        };
    }

    private async Task SaveAnalysisAsync(string userId, ProfileInput input, ProfessionalProfileAnalysis analysis)
    {
        var profile = await _db.ProfessionalProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (profile is null)
        {
            profile = new ProfessionalProfile { UserId = userId };
            _db.ProfessionalProfiles.Add(profile);
        }

        profile.DisplayName = User.Identity?.Name;
        profile.CurrentProfession = Trim(input.CurrentProfession, 140);
        profile.Headline = Trim(analysis.GeneratedAssets.LinkedInHeadline, 220);
        profile.ExperienceSummary = Trim(input.Experience, 3000);
        profile.TechnicalSkills = Trim(input.TechnicalSkills, 1000);
        profile.SoftSkills = Trim(input.SoftSkills, 1000);
        profile.Tools = Trim(input.Tools, 1000);
        profile.Education = Trim(input.Education, 1000);
        profile.Languages = Trim(input.Languages, 500);
        profile.DesiredLocation = Trim(input.DesiredLocation, 180);
        profile.WorkMode = Trim(input.WorkMode, 40);
        profile.EngagementType = Trim(input.EngagementType, 80);
        profile.CompensationGoal = Trim(input.CompensationGoal, 160);
        profile.InterestAreas = Trim(input.InterestAreas, 1000);
        profile.PortfolioUrl = Trim(input.PortfolioUrl, 500);
        profile.ProfilePhotoPath = Trim(input.ProfilePhotoLabel, 500);
        profile.IsOpenToEmployment = true;
        profile.IsOpenToFreelance = true;
        profile.IsAvailableForServices = true;

        await _db.SaveChangesAsync();

        await ReplaceSkillsAsync(profile.Id, input, analysis);
        await SaveJobMatchesAsync(profile.Id, analysis.JobOpportunities);
        await SaveFreelanceOffersAsync(profile.Id, input, analysis.FreelanceOpportunities);
        await SaveCareerPlanAsync(profile.Id, analysis.CareerPlan);
        await ReplaceGeneratedAssetsAsync(profile.Id, analysis.GeneratedAssets);
        await _db.SaveChangesAsync();
    }

    private async Task ReplaceSkillsAsync(int profileId, ProfileInput input, ProfessionalProfileAnalysis analysis)
    {
        var existing = await _db.ProfessionalSkills
            .Where(x => x.ProfessionalProfileId == profileId)
            .ToListAsync();
        _db.ProfessionalSkills.RemoveRange(existing);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddSkills(profileId, "Technical", input.TechnicalSkills, false, seen);
        AddSkills(profileId, "Soft", input.SoftSkills, false, seen);
        AddSkills(profileId, "Tools", input.Tools, false, seen);
        AddSkills(profileId, "Languages", input.Languages, false, seen);

        foreach (var skill in analysis.SuggestedSkills.Where(skill => seen.Add(skill)))
        {
            _db.ProfessionalSkills.Add(new ProfessionalSkill
            {
                ProfessionalProfileId = profileId,
                Name = Trim(skill, 120),
                Category = "Suggested",
                IsAiSuggested = true,
                IsConfirmedByUser = false
            });
        }
    }

    private void AddSkills(int profileId, string category, string rawSkills, bool aiSuggested, HashSet<string> seen)
    {
        foreach (var skill in SplitTerms(rawSkills).Where(seen.Add))
        {
            _db.ProfessionalSkills.Add(new ProfessionalSkill
            {
                ProfessionalProfileId = profileId,
                Name = Trim(skill, 120),
                Category = category,
                IsAiSuggested = aiSuggested,
                IsConfirmedByUser = true
            });
        }
    }

    private async Task SaveJobMatchesAsync(int profileId, IReadOnlyList<OpportunityRecommendation> opportunities)
    {
        foreach (var item in opportunities.Take(8))
        {
            var externalId = Slug($"{item.Type}-{item.Organization}-{item.Title}-{item.Location}");
            var opportunity = await _db.WorkOpportunities
                .FirstOrDefaultAsync(x => x.Source == "Workvert AI" && x.ExternalId == externalId);

            if (opportunity is null)
            {
                opportunity = new WorkOpportunity
                {
                    Source = "Workvert AI",
                    ExternalId = externalId
                };
                _db.WorkOpportunities.Add(opportunity);
                await _db.SaveChangesAsync();
            }

            opportunity.Title = Trim(item.Title, 180);
            opportunity.Organization = Trim(item.Organization, 180);
            opportunity.OpportunityType = Trim(item.Type, 40);
            opportunity.Location = Trim(item.Location, 180);
            opportunity.WorkMode = Trim(item.WorkMode, 40);
            opportunity.RequiredSkills = Trim(string.Join(", ", item.MatchedSkills.Concat(item.MissingSkills).Distinct(StringComparer.OrdinalIgnoreCase)), 1200);
            opportunity.NiceToHaveSkills = Trim(string.Join(", ", item.MissingSkills), 1200);
            opportunity.Description = Trim(string.Join(" ", item.MatchReasons), 4000);
            opportunity.CompensationPeriod = Trim(item.CompensationRange, 40);
            opportunity.Status = "Active";
            opportunity.LastSeenAtUtc = DateTime.UtcNow;

            var match = await _db.ProfessionalOpportunityMatches
                .FirstOrDefaultAsync(x => x.ProfessionalProfileId == profileId && x.WorkOpportunityId == opportunity.Id);

            if (match is null)
            {
                match = new ProfessionalOpportunityMatch
                {
                    ProfessionalProfileId = profileId,
                    WorkOpportunityId = opportunity.Id
                };
                _db.ProfessionalOpportunityMatches.Add(match);
            }

            match.CompatibilityScore = item.CompatibilityScore;
            match.MatchType = item.Type.Contains("Freelance", StringComparison.OrdinalIgnoreCase) ? "Freelance" : "Employment";
            match.RecommendationReasons = Trim(string.Join(" | ", item.MatchReasons), 2000);
            match.MatchedSkills = Trim(string.Join(", ", item.MatchedSkills), 1000);
            match.MissingSkills = Trim(string.Join(", ", item.MissingSkills), 1000);
            match.SuggestedNextStep = Trim(item.SuggestedAction, 1000);
            match.Status = "Suggested";
        }
    }

    private async Task SaveFreelanceOffersAsync(int profileId, ProfileInput input, IReadOnlyList<FreelanceRecommendation> offers)
    {
        var activeOffers = await _db.FreelancerServiceListings
            .Where(x => x.ProfessionalProfileId == profileId && x.IsActive)
            .ToListAsync();

        foreach (var offer in activeOffers)
        {
            offer.IsActive = false;
        }

        foreach (var item in offers.Take(8))
        {
            _db.FreelancerServiceListings.Add(new FreelancerServiceListing
            {
                ProfessionalProfileId = profileId,
                ServiceName = Trim(item.Service, 160),
                Category = "Generated offer",
                Description = Trim($"{item.ProposalAngle} Next step: {item.SuggestedAction}", 2500),
                Skills = Trim(string.Join(", ", item.RequiredSkills), 1200),
                Currency = DetectCurrency(item.PriceSuggestion),
                Location = Trim(input.DesiredLocation, 180),
                RemoteAvailable = !input.WorkMode.Equals("On-site", StringComparison.OrdinalIgnoreCase),
                Availability = "To confirm",
                PortfolioUrl = Trim(input.PortfolioUrl, 500),
                IsActive = true
            });
        }
    }

    private async Task SaveCareerPlanAsync(int profileId, CareerPlan plan)
    {
        _db.CareerActionPlans.Add(new CareerActionPlan
        {
            ProfessionalProfileId = profileId,
            TargetRole = "Profile growth plan",
            Summary = Trim(plan.Positioning, 2500),
            SkillGaps = Trim(string.Join("\n", plan.SkillGaps), 1500),
            RecommendedLearning = Trim(string.Join("\n", plan.RecommendedLearning), 2000),
            CvAdvice = Trim(string.Join("\n", plan.CvImprovements), 2000),
            LinkedInAdvice = "Keep LinkedIn aligned with the target path and strongest evidence.",
            NextSteps = Trim(string.Join("\n", plan.NextSteps), 2500)
        });

        await Task.CompletedTask;
    }

    private async Task ReplaceGeneratedAssetsAsync(int profileId, GeneratedProfileAssets assets)
    {
        var existing = await _db.GeneratedProfessionalAssets
            .Where(x => x.ProfessionalProfileId == profileId)
            .ToListAsync();
        _db.GeneratedProfessionalAssets.RemoveRange(existing);

        AddAsset(profileId, "CV summary", "Resume summary", assets.CvSummary);
        AddAsset(profileId, "LinkedIn headline", "LinkedIn headline", assets.LinkedInHeadline);
        AddAsset(profileId, "Professional bio", "Professional bio", assets.ProfessionalBio);
        AddAsset(profileId, "Application message", "Application message", assets.CoverMessage);
        AddAsset(profileId, "Freelance pitch", "Freelance pitch", assets.FreelancePitch);
    }

    private void AddAsset(int profileId, string type, string title, string content)
    {
        _db.GeneratedProfessionalAssets.Add(new GeneratedProfessionalAsset
        {
            ProfessionalProfileId = profileId,
            AssetType = Trim(type, 60),
            Title = Trim(title, 120),
            Content = Trim(content, 8000),
            Language = "English"
        });
    }

    private async Task LoadSavedSummaryAsync(string userId)
    {
        var profile = await _db.ProfessionalProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (profile is null)
        {
            ProfileCompletion = 0;
            return;
        }

        LastProfileUpdateUtc = profile.UpdatedAtUtc;
        ProfileCompletion = CalculateCompletion(profile);
        SavedJobMatches = await _db.ProfessionalOpportunityMatches
            .AsNoTracking()
            .Where(x => x.ProfessionalProfileId == profile.Id && x.Status == "Suggested")
            .CountAsync();
        SavedServiceOffers = await _db.FreelancerServiceListings
            .AsNoTracking()
            .Where(x => x.ProfessionalProfileId == profile.Id && x.IsActive)
            .CountAsync();
        SavedAssets = await _db.GeneratedProfessionalAssets
            .AsNoTracking()
            .Where(x => x.ProfessionalProfileId == profile.Id)
            .CountAsync();
    }

    private static int CalculateCompletion(ProfessionalProfile profile)
    {
        var checks = new[]
        {
            profile.CurrentProfession,
            profile.ExperienceSummary,
            profile.TechnicalSkills,
            profile.Tools,
            profile.Languages,
            profile.DesiredLocation,
            profile.WorkMode,
            profile.EngagementType,
            profile.InterestAreas,
            profile.PortfolioUrl
        };

        return Math.Clamp((int)Math.Round(checks.Count(x => !string.IsNullOrWhiteSpace(x)) / (double)checks.Length * 100), 0, 100);
    }

    private static string NormalizePath(string? path)
    {
        return string.Equals(path, "freelance", StringComparison.OrdinalIgnoreCase) ? "freelance" : "job";
    }

    private static IReadOnlyList<string> SplitTerms(string? value)
    {
        return (value ?? string.Empty)
            .Split([',', ';', '\n', '\r'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(40)
            .ToList();
    }

    private static string Slug(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-');
            }
        }

        return Regex.Replace(builder.ToString(), "-+", "-").Trim('-');
    }

    private static string DetectCurrency(string value)
    {
        return value.Contains("EUR", StringComparison.OrdinalIgnoreCase) || value.Contains('€') ? "EUR" : "EUR";
    }

    private static string Trim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    public class ProfileInput
    {
        [StringLength(40)]
        public string Path { get; set; } = "job";

        [Required]
        [StringLength(120)]
        [Display(Name = "Current profession")]
        public string CurrentProfession { get; set; } = string.Empty;

        [StringLength(1500)]
        [Display(Name = "Professional experience")]
        public string Experience { get; set; } = string.Empty;

        [StringLength(600)]
        [Display(Name = "Technical skills")]
        public string TechnicalSkills { get; set; } = string.Empty;

        [StringLength(600)]
        [Display(Name = "Soft skills")]
        public string SoftSkills { get; set; } = string.Empty;

        [StringLength(600)]
        [Display(Name = "Software and tools")]
        public string Tools { get; set; } = string.Empty;

        [StringLength(600)]
        [Display(Name = "Education")]
        public string Education { get; set; } = string.Empty;

        [StringLength(300)]
        [Display(Name = "Languages")]
        public string Languages { get; set; } = string.Empty;

        [StringLength(180)]
        [Display(Name = "Desired location")]
        public string DesiredLocation { get; set; } = string.Empty;

        [StringLength(80)]
        [Display(Name = "Work mode")]
        public string WorkMode { get; set; } = "Remote";

        [StringLength(120)]
        [Display(Name = "Opportunity type")]
        public string EngagementType { get; set; } = "Full-time job";

        [StringLength(120)]
        [Display(Name = "Target salary or hourly rate")]
        public string CompensationGoal { get; set; } = string.Empty;

        [StringLength(600)]
        [Display(Name = "Professional areas of interest")]
        public string InterestAreas { get; set; } = string.Empty;

        [StringLength(300)]
        [Display(Name = "Portfolio or LinkedIn")]
        public string? PortfolioUrl { get; set; }

        [StringLength(120)]
        [Display(Name = "Professional photo or avatar")]
        public string? ProfilePhotoLabel { get; set; }

        public ProfessionalProfileRequest ToRequest()
        {
            return new ProfessionalProfileRequest(
                CurrentProfession,
                Experience,
                TechnicalSkills,
                SoftSkills,
                Tools,
                Education,
                Languages,
                DesiredLocation,
                WorkMode,
                EngagementType,
                CompensationGoal,
                InterestAreas,
                PortfolioUrl,
                ProfilePhotoLabel);
        }
    }
}
