using System.ComponentModel.DataAnnotations;

namespace Workvert.Models;

public class WorkOpportunity
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Source { get; set; } = "Manual";

    [StringLength(160)]
    public string? ExternalId { get; set; }

    [StringLength(500)]
    public string? SourceUrl { get; set; }

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [StringLength(180)]
    public string? Organization { get; set; }

    [Required, StringLength(40)]
    public string OpportunityType { get; set; } = "Full-time job";

    [StringLength(180)]
    public string? Location { get; set; }

    [Required, StringLength(40)]
    public string WorkMode { get; set; } = "Flexible";

    [StringLength(1200)]
    public string? RequiredSkills { get; set; }

    [StringLength(1200)]
    public string? NiceToHaveSkills { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    public decimal? CompensationMin { get; set; }
    public decimal? CompensationMax { get; set; }

    [StringLength(3)]
    public string? Currency { get; set; }

    [StringLength(40)]
    public string? CompensationPeriod { get; set; }

    [Required, StringLength(32)]
    public string Status { get; set; } = "Active";

    public DateTime? PublishedAtUtc { get; set; }
    public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<ProfessionalOpportunityMatch> Matches { get; set; } = new();
}

