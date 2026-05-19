using System.ComponentModel.DataAnnotations;

namespace Workvert.Models;

public class GeneratedProfessionalAsset
{
    public int Id { get; set; }

    public int ProfessionalProfileId { get; set; }

    [Required, StringLength(60)]
    public string AssetType { get; set; } = "Profile bio";

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(8000)]
    public string Content { get; set; } = string.Empty;

    [StringLength(80)]
    public string? Language { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ProfessionalProfile? ProfessionalProfile { get; set; }
}
