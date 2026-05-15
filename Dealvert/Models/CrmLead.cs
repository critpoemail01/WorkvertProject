using System.ComponentModel.DataAnnotations;

namespace Alivert.Models;

public class CrmLead
{
    public int Id { get; set; }

    [Required, StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [StringLength(120)]
    public string? ExternalId { get; set; }

    [Required, StringLength(160)]
    public string ContactName { get; set; } = string.Empty;

    [Required, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(80)]
    public string? Phone { get; set; }

    [StringLength(180)]
    public string? CompanyName { get; set; }

    [StringLength(120)]
    public string? Role { get; set; }

    [StringLength(120)]
    public string? Industry { get; set; }

    [StringLength(120)]
    public string? Country { get; set; }

    [StringLength(160)]
    public string? City { get; set; }

    [StringLength(120)]
    public string? Stage { get; set; }

    [StringLength(300)]
    public string? Tags { get; set; }

    [StringLength(120)]
    public string? Source { get; set; }

    [Required, StringLength(24)]
    public string ConsentStatus { get; set; } = "Unknown";

    [StringLength(180)]
    public string? ConsentSource { get; set; }

    public DateTime? ConsentedAtUtc { get; set; }
    public DateTime? UnsubscribedAtUtc { get; set; }

    [StringLength(800)]
    public string? Notes { get; set; }

    [Required, StringLength(24)]
    public string Status { get; set; } = "Imported";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncedAtUtc { get; set; }
}
