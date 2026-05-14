using System.ComponentModel.DataAnnotations;

namespace Alivert.Models;

public class MarketingLandingPage
{
    public int Id { get; set; }

    public int MarketingPlanId { get; set; }
    public MarketingPlan? MarketingPlan { get; set; }

    [Required, StringLength(120)]
    public string Slug { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string Headline { get; set; } = string.Empty;

    [Required, StringLength(260)]
    public string Subheadline { get; set; } = string.Empty;

    [Required, StringLength(1600)]
    public string Body { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string PrimaryCallToAction { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string FormTitle { get; set; } = string.Empty;

    [Required, StringLength(260)]
    public string FormIntro { get; set; } = string.Empty;

    [Required, StringLength(260)]
    public string ThankYouMessage { get; set; } = string.Empty;

    [Required, StringLength(24)]
    public string Status { get; set; } = "Draft";

    public int ViewCount { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }

    public List<MarketingLandingLead> Leads { get; set; } = new();
}
