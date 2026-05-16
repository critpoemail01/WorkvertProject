using System.ComponentModel.DataAnnotations;

namespace Dealvert.Models;

public class Alert
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(180, ErrorMessage = "Source must be 180 characters or fewer.")]
    public string Symbol { get; set; } = "https://example.com";

    [Required]
    public MarketType MarketType { get; set; } = MarketType.Crypto;

    [Required]
    public AlertRuleType RuleType { get; set; }

    // Reused by Dealvert as the product source URL.
    [Required]
    public decimal Threshold { get; set; }

    [Required, StringLength(8)]
    public string Timeframe { get; set; } = "1wk";

    [Range(0.01, 100)]
    public decimal ZonePercent { get; set; } = 1.0m;

    public bool PriceZoneWasInside { get; set; }

    [Range(2, 100)]
    public int RsiPeriod { get; set; } = 14;

    [Range(1, 200)]
    public int FastEmaPeriod { get; set; } = 3;

    [Range(2, 250)]
    public int SlowEmaPeriod { get; set; } = 5;

    public bool IndicatorArmed { get; set; }

    public decimal? LastIndicatorValue { get; set; }

    public DateTime? LastEvaluatedAtUtc { get; set; }

    [Range(1, 1440)]
    public int CooldownMinutes { get; set; } = 240;

    public bool IsEnabled { get; set; } = true;

    public DateTime? LastTriggeredAtUtc { get; set; }

    [StringLength(32)]
    public string Channel { get; set; } = "Email";

    [StringLength(4000)]
    public string? AudienceList { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
