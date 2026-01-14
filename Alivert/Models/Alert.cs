using System.ComponentModel.DataAnnotations;

namespace Alivert.Models;

public class Alert
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(24), RegularExpression(@"^[A-Z0-9._-]{1,24}$", ErrorMessage = "Symbol may contain A-Z, 0-9, dot, underscore, dash only.")]
    public string Symbol { get; set; } = "AAPL";

    [Required]
    public AlertRuleType RuleType { get; set; }

    // Example: 180.50 for PriceAbove/Below; -3 for PercentDrop24h
    [Required]
    public decimal Threshold { get; set; }

    [Range(1, 1440)]
    public int CooldownMinutes { get; set; } = 240;

    public bool IsEnabled { get; set; } = true;

    public DateTime? LastTriggeredAtUtc { get; set; }

    [StringLength(32)]
    public string Channel { get; set; } = "Email";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
