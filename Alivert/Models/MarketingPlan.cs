using System.ComponentModel.DataAnnotations;

namespace Alivert.Models;

public class MarketingPlan
{
    public int Id { get; set; }

    [Required, StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string ProductName { get; set; } = string.Empty;

    [StringLength(300)]
    public string? ProductUrl { get; set; }

    [Required, StringLength(400)]
    public string CompanyOrIdea { get; set; } = string.Empty;

    [Required, StringLength(700)]
    public string TargetAudience { get; set; } = string.Empty;

    [Required, StringLength(700)]
    public string ValueProposition { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string CampaignGoal { get; set; } = "Subscriptions";

    [Required, StringLength(80)]
    public string Tone { get; set; } = "Clear and practical";

    [Required, StringLength(300)]
    public string Platforms { get; set; } = "TikTok,Instagram,Facebook,LinkedIn";

    [Required, StringLength(16)]
    public string AudienceLocationScope { get; set; } = "World";

    [StringLength(120)]
    public string? AudienceCountry { get; set; }

    [StringLength(160)]
    public string? AudienceCity { get; set; }

    public double? AudienceLatitude { get; set; }

    public double? AudienceLongitude { get; set; }

    public int? AudienceRadiusKm { get; set; }

    [Required, StringLength(32)]
    public string Frequency { get; set; } = "Daily";

    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

    [StringLength(4000)]
    public string? EmailAudience { get; set; }

    [Required, StringLength(32)]
    public string Status { get; set; } = "Draft";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<MarketingPostSuggestion> Posts { get; set; } = new();
    public List<MarketingEmailSuggestion> Emails { get; set; } = new();
    public List<MarketingLeadSuggestion> Leads { get; set; } = new();
}
