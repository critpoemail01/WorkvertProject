using System.ComponentModel.DataAnnotations;

namespace Workvert.Models;

public class CreditTransaction
{
    public int Id { get; set; }

    [Required, StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    public int Delta { get; set; }

    [Required, StringLength(80)]
    public string Reason { get; set; } = string.Empty;

    [StringLength(120)]
    public string? Reference { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
