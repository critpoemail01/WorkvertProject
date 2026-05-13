using System.ComponentModel.DataAnnotations;

namespace Alivert.Models;

public class MarketingPostSuggestion
{
    public int Id { get; set; }

    public int MarketingPlanId { get; set; }
    public MarketingPlan? MarketingPlan { get; set; }

    [Required, StringLength(40)]
    public string Platform { get; set; } = string.Empty;

    public DateTime ScheduledForUtc { get; set; }

    public int DayNumber { get; set; }

    [Required, StringLength(140)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string Hook { get; set; } = string.Empty;

    [Required, StringLength(1600)]
    public string Caption { get; set; } = string.Empty;

    [Required, StringLength(900)]
    public string CreativeBrief { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Hashtags { get; set; }

    [Required, StringLength(180)]
    public string CallToAction { get; set; } = string.Empty;

    [Required, StringLength(24)]
    public string Status { get; set; } = "Draft";

    public int EstimatedReach { get; set; }
    public int EstimatedInteractions { get; set; }
    public int EstimatedConversions { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}
