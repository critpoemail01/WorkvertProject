using System.ComponentModel.DataAnnotations;

namespace Workvert.Models;

public class ProfessionalSkill
{
    public int Id { get; set; }

    public int ProfessionalProfileId { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Category { get; set; } = "Technical";

    [StringLength(40)]
    public string? ProficiencyLevel { get; set; }

    [Range(0, 60)]
    public int? YearsExperience { get; set; }

    public bool IsAiSuggested { get; set; }
    public bool IsConfirmedByUser { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ProfessionalProfile? ProfessionalProfile { get; set; }
}

