namespace Alivert.Services;

public interface IUserAccountService
{
    /// <summary>
    /// Ensures the user account exists. Creates it with 5 free credits if missing.
    /// </summary>
    Task EnsureAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Returns (isUnlimited, allowedAlertCapacity, activeAlerts, remainingSlots).
    /// Credits represent capacity (max active alerts).
    /// </summary>
    Task<(bool IsUnlimited, int Capacity, int ActiveAlerts, int RemainingSlots)> GetLimitsAsync(string userId, CancellationToken ct = default);

    Task AddCreditsAsync(string userId, int credits, string reason, string? reference = null, CancellationToken ct = default);

    Task ActivateUnlimitedAsync(string userId, TimeSpan duration, string reason, string? reference = null, CancellationToken ct = default);
}
