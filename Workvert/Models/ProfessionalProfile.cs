using System.ComponentModel.DataAnnotations;

namespace Workvert.Models;

public class ProfessionalProfile
{
    public int Id { get; set; }

    [Required, StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [StringLength(120)]
    public string? DisplayName { get; set; }

    [Required, StringLength(140)]
    public string CurrentProfession { get; set; } = string.Empty;

    [StringLength(220)]
    public string? Headline { get; set; }

    [StringLength(3000)]
    public string? ExperienceSummary { get; set; }

    [StringLength(1000)]
    public string? TechnicalSkills { get; set; }

    [StringLength(1000)]
    public string? SoftSkills { get; set; }

    [StringLength(1000)]
    public string? Tools { get; set; }

    [StringLength(1000)]
    public string? Education { get; set; }

    [StringLength(500)]
    public string? Languages { get; set; }

    [StringLength(180)]
    public string? DesiredLocation { get; set; }

    [Required, StringLength(40)]
    public string WorkMode { get; set; } = "Flexible";

    [Required, StringLength(80)]
    public string EngagementType { get; set; } = "Full-time and freelance";

    [StringLength(160)]
    public string? CompensationGoal { get; set; }

    [StringLength(1000)]
    public string? InterestAreas { get; set; }

    [StringLength(500)]
    public string? PortfolioUrl { get; set; }

    [StringLength(500)]
    public string? ProfilePhotoPath { get; set; }

    [Required, StringLength(260)]
    public string ProfilePhotoPurposeNote { get; set; } = "Profile photos are only used for profile presentation, CV visuals or professional avatars.";

    public bool IsOpenToEmployment { get; set; } = true;
    public bool IsOpenToFreelance { get; set; } = true;
    public bool IsAvailableForServices { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<ProfessionalSkill> Skills { get; set; } = new();
    public List<ProfessionalOpportunityMatch> OpportunityMatches { get; set; } = new();
    public List<FreelancerServiceListing> ServiceListings { get; set; } = new();
    public List<CareerActionPlan> CareerActionPlans { get; set; } = new();
    public List<GeneratedProfessionalAsset> GeneratedAssets { get; set; } = new();
}

