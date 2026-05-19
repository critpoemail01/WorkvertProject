using System.ComponentModel.DataAnnotations;

namespace Workvert.Models;

public class ProfessionalOpportunityMatch
{
    public int Id { get; set; }

    public int ProfessionalProfileId { get; set; }
    public int WorkOpportunityId { get; set; }

    [Range(0, 100)]
    public int CompatibilityScore { get; set; }

    [Required, StringLength(40)]
    public string MatchType { get; set; } = "Employment";

    [StringLength(2000)]
    public string? RecommendationReasons { get; set; }

    [StringLength(1000)]
    public string? MatchedSkills { get; set; }

    [StringLength(1000)]
    public string? MissingSkills { get; set; }

    [StringLength(1000)]
    public string? SuggestedNextStep { get; set; }

    [Required, StringLength(32)]
    public string Status { get; set; } = "Suggested";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? NotifiedAtUtc { get; set; }
    public DateTime? DismissedAtUtc { get; set; }

    public ProfessionalProfile? ProfessionalProfile { get; set; }
    public WorkOpportunity? WorkOpportunity { get; set; }
}

