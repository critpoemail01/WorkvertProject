using Dealvert.Data;
using Dealvert.Models;
using Microsoft.EntityFrameworkCore;

namespace Dealvert.Services;

public sealed class UserAccountService : IUserAccountService
{
    private const int FreeCreditCapacity = 5;
    private const int PaidCreditValidityDays = 30;
    private readonly ApplicationDbContext _db;

    public UserAccountService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task EnsureAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        var existing = await _db.UserAccounts.AsNoTracking().AnyAsync(x => x.UserId == userId, ct);
        if (existing)
            return;

        _db.UserAccounts.Add(new UserAccount
        {
            UserId = userId,
            Credits = 5,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<(bool IsUnlimited, int Capacity, int ActiveAlerts, int RemainingSlots)> GetLimitsAsync(string userId, CancellationToken ct = default)
    {
        var (isUnlimited, capacity) = await GetCapacityAsync(userId, ct);
        await DisableExcessAlertsAsync(userId, isUnlimited, capacity, ct);

        var active = await CountActiveCreditUnitsAsync(userId, ct);
        var remaining = isUnlimited ? int.MaxValue : Math.Max(0, capacity - active);

        return (isUnlimited, capacity, active, remaining);
    }

    public async Task<int> EnforceActiveAlertLimitAsync(string userId, CancellationToken ct = default)
    {
        var (isUnlimited, capacity) = await GetCapacityAsync(userId, ct);
        return await DisableExcessAlertsAsync(userId, isUnlimited, capacity, ct);
    }

    public async Task AddCreditsAsync(string userId, int credits, string reason, string? reference = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || credits <= 0)
            return;

        await EnsureAsync(userId, ct);

        if (!string.IsNullOrWhiteSpace(reference))
        {
            var existingTransaction = await _db.CreditTransactions
                .AsNoTracking()
                .AnyAsync(x => x.Reference == reference, ct);
            if (existingTransaction)
                return;
        }

        var account = await _db.UserAccounts.FirstAsync(x => x.UserId == userId, ct);
        account.Credits += credits;
        account.UpdatedAtUtc = DateTime.UtcNow;

        _db.CreditTransactions.Add(new CreditTransaction
        {
            UserId = userId,
            Delta = credits,
            Reason = reason,
            Reference = reference
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task ActivateUnlimitedAsync(string userId, TimeSpan duration, string reason, string? reference = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || duration <= TimeSpan.Zero)
            return;

        await EnsureAsync(userId, ct);

        if (!string.IsNullOrWhiteSpace(reference))
        {
            var existingTransaction = await _db.CreditTransactions
                .AsNoTracking()
                .AnyAsync(x => x.Reference == reference, ct);
            if (existingTransaction)
                return;
        }

        var account = await _db.UserAccounts.FirstAsync(x => x.UserId == userId, ct);
        var startsAt = account.UnlimitedUntilUtc is not null && account.UnlimitedUntilUtc.Value > DateTime.UtcNow
            ? account.UnlimitedUntilUtc.Value
            : DateTime.UtcNow;

        account.UnlimitedUntilUtc = startsAt.Add(duration);
        account.UpdatedAtUtc = DateTime.UtcNow;

        _db.CreditTransactions.Add(new CreditTransaction
        {
            UserId = userId,
            Delta = 0,
            Reason = reason,
            Reference = reference
        });

        await _db.SaveChangesAsync(ct);
    }

    private async Task<(bool IsUnlimited, int Capacity)> GetCapacityAsync(string userId, CancellationToken ct)
    {
        await EnsureAsync(userId, ct);

        var acc = await _db.UserAccounts.AsNoTracking().FirstAsync(x => x.UserId == userId, ct);
        var nowUtc = DateTime.UtcNow;
        var isUnlimited = acc.UnlimitedUntilUtc is not null && acc.UnlimitedUntilUtc.Value > nowUtc;
        if (isUnlimited)
            return (true, int.MaxValue);

        var activePaidCredits = await _db.CreditTransactions
            .AsNoTracking()
            .Where(x => x.UserId == userId &&
                        x.Delta > 0 &&
                        x.CreatedAtUtc >= nowUtc.AddDays(-PaidCreditValidityDays))
            .SumAsync(x => x.Delta, ct);

        return (false, FreeCreditCapacity + activePaidCredits);
    }

    private async Task<int> DisableExcessAlertsAsync(string userId, bool isUnlimited, int capacity, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId) || isUnlimited)
            return 0;

        var activeAlertCount = await _db.Alerts.CountAsync(a => a.UserId == userId && a.IsEnabled, ct);
        if (activeAlertCount <= capacity)
            return 0;

        var nowUtc = DateTime.UtcNow;
        var alertsToDisable = await _db.Alerts
            .Where(a => a.UserId == userId && a.IsEnabled)
            .OrderByDescending(a => a.UpdatedAtUtc)
            .ThenByDescending(a => a.Id)
            .Skip(capacity)
            .ToListAsync(ct);

        foreach (var alert in alertsToDisable)
        {
            alert.IsEnabled = false;
            alert.UpdatedAtUtc = nowUtc;
        }

        await _db.SaveChangesAsync(ct);
        return alertsToDisable.Count;
    }

    private async Task<int> CountActiveCreditUnitsAsync(string userId, CancellationToken ct)
    {
        var activeProductAlerts = await _db.Alerts
            .AsNoTracking()
            .CountAsync(a => a.UserId == userId && a.IsEnabled, ct);

        return activeProductAlerts;
    }
}
