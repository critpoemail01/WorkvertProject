using System.ComponentModel.DataAnnotations;

namespace Dealvert.Models;

public class AlertDeliveryLog
{
    public int Id { get; set; }

    public int? AlertTriggerId { get; set; }
    public AlertTrigger? AlertTrigger { get; set; }

    public int AlertId { get; set; }

    [Required, StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string Symbol { get; set; } = string.Empty;

    [Required, StringLength(32)]
    public string Channel { get; set; } = string.Empty;

    [Required, StringLength(16)]
    public string Status { get; set; } = "Skipped";

    [StringLength(120)]
    public string? Destination { get; set; }

    public int? ResponseStatusCode { get; set; }

    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
