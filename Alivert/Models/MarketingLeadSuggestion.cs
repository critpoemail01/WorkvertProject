using System.ComponentModel.DataAnnotations;

namespace Alivert.Models;

public class MarketingLeadSuggestion
{
    public int Id { get; set; }

    public int MarketingPlanId { get; set; }
    public MarketingPlan? MarketingPlan { get; set; }

    [Required, StringLength(160)]
    public string CompanyProfile { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Industry { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string ContactRole { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string EmailSearchHint { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Required, StringLength(24)]
    public string Status { get; set; } = "Suggested";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
