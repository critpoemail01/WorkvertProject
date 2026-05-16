using System.ComponentModel.DataAnnotations;

namespace Workvert.Models;

/// <summary>
/// Simple user billing/limits profile.
/// Credits stores the current account credit snapshot; active paid capacity is calculated from recent credit transactions.
/// If UnlimitedUntilUtc is set and in the future, the user is unlimited.
/// </summary>
public class UserAccount
{
    [Key]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Current credit snapshot. Free capacity is 5; paid credit capacity expires from the related transaction window.
    /// </summary>
    [Range(0, 1000000)]
    public int Credits { get; set; } = 5;

    /// <summary>
    /// If set to a future date, user has unlimited alerts until this moment.
    /// </summary>
    public DateTime? UnlimitedUntilUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
