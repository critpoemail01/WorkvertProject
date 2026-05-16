using System.ComponentModel.DataAnnotations;

namespace Workvert.Models;

public class AlertTrigger
{
    public int Id { get; set; }

    public int AlertId { get; set; }
    public Alert? Alert { get; set; }

    public DateTime TriggeredAtUtc { get; set; } = DateTime.UtcNow;

    [Required, StringLength(500)]
    public string Message { get; set; } = string.Empty;

    // For debugging/audit (MVP)
    [StringLength(4000)]
    public string? SnapshotJson { get; set; }
}
