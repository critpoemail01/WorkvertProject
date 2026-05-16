using System.ComponentModel.DataAnnotations;

namespace Workvert.Models;

public class MarketingLandingLead
{
    public int Id { get; set; }

    public int MarketingLandingPageId { get; set; }
    public MarketingLandingPage? MarketingLandingPage { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(80)]
    public string? Phone { get; set; }

    [StringLength(160)]
    public string? Company { get; set; }

    [StringLength(120)]
    public string? Role { get; set; }

    [StringLength(800)]
    public string? Message { get; set; }

    [StringLength(120)]
    public string? Source { get; set; }

    public bool MarketingConsentAccepted { get; set; }

    [StringLength(300)]
    public string? ConsentText { get; set; }

    public DateTime? ConsentedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
