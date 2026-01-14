using Alivert.Data;
using Alivert.Models;
using Microsoft.EntityFrameworkCore;

namespace Alivert.Services;

public sealed class UserAccountService : IUserAccountService
{
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
        await EnsureAsync(userId, ct);

        var acc = await _db.UserAccounts.AsNoTracking().FirstAsync(x => x.UserId == userId, ct);

        var isUnlimited = acc.UnlimitedUntilUtc is not null && acc.UnlimitedUntilUtc.Value > DateTime.UtcNow;
        var capacity = isUnlimited ? int.MaxValue : Math.Max(0, acc.Credits);

        var active = await _db.Alerts.AsNoTracking().CountAsync(a => a.UserId == userId && a.IsEnabled, ct);
        var remaining = isUnlimited ? int.MaxValue : Math.Max(0, capacity - active);

        return (isUnlimited, capacity, active, remaining);
    }
}
