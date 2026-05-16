using System.ComponentModel.DataAnnotations;

namespace Workvert.Models;

public class MarketingEmailSuggestion
{
    public int Id { get; set; }

    public int MarketingPlanId { get; set; }
    public MarketingPlan? MarketingPlan { get; set; }

    public DateTime ScheduledForUtc { get; set; }

    public int DayNumber { get; set; }

    [Required, StringLength(160)]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(220)]
    public string PreviewText { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string Body { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string AudienceSegment { get; set; } = string.Empty;

    [Required, StringLength(24)]
    public string Status { get; set; } = "Draft";

    public int EstimatedReach { get; set; }
    public int EstimatedInteractions { get; set; }
    public int EstimatedConversions { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
}
