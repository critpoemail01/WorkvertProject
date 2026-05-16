using System.ComponentModel.DataAnnotations;

namespace Workvert.Models;

public class CrmIntegration
{
    public int Id { get; set; }

    [Required, StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Provider { get; set; } = "CSV / manual import";

    [Required, StringLength(120)]
    public string DisplayName { get; set; } = "Trusted source list";

    [StringLength(500)]
    public string? ApiBaseUrl { get; set; }

    [StringLength(80)]
    public string? ApiKeyHint { get; set; }

    [Required, StringLength(24)]
    public string Status { get; set; } = "Configured";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastImportedAtUtc { get; set; }
}
