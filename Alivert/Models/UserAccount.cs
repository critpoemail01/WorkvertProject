using System.ComponentModel.DataAnnotations;

namespace Alivert.Models;

/// <summary>
/// Simple user billing/limits profile.
/// Credits represent the maximum number of active alerts the user may have.
/// If UnlimitedUntilUtc is set and in the future, the user is unlimited.
/// </summary>
public class UserAccount
{
    [Key]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of active alerts allowed (capacity).
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
