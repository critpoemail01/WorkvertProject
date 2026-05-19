using System.ComponentModel.DataAnnotations;

namespace Workvert.Models;

public class CareerActionPlan
{
    public int Id { get; set; }

    public int ProfessionalProfileId { get; set; }

    [Required, StringLength(160)]
    public string TargetRole { get; set; } = string.Empty;

    [StringLength(2500)]
    public string? Summary { get; set; }

    [StringLength(1500)]
    public string? SkillGaps { get; set; }

    [StringLength(2000)]
    public string? RecommendedLearning { get; set; }

    [StringLength(2000)]
    public string? CvAdvice { get; set; }

    [StringLength(2000)]
    public string? LinkedInAdvice { get; set; }

    [StringLength(1000)]
    public string? SalaryInsight { get; set; }

    [StringLength(2500)]
    public string? NextSteps { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ProfessionalProfile? ProfessionalProfile { get; set; }
}

