using Alivert.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Alivert.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<AlertTrigger> AlertTriggers => Set<AlertTrigger>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    
    public override int SaveChanges()
    {
        TouchTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        TouchTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void TouchTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is Alert alert)
            {
                if (entry.State == EntityState.Added)
                {
                    if (alert.CreatedAtUtc == default) alert.CreatedAtUtc = now;
                    alert.UpdatedAtUtc = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    alert.UpdatedAtUtc = now;
                }
            }
            else if (entry.Entity is UserAccount ua)
            {
                if (entry.State == EntityState.Added)
                {
                    if (ua.CreatedAtUtc == default) ua.CreatedAtUtc = now;
                    ua.UpdatedAtUtc = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    ua.UpdatedAtUtc = now;
                }
            }
        }
    }

protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Alert>()
            .HasIndex(a => new { a.UserId, a.Symbol });

        builder.Entity<Alert>()
            .HasIndex(a => new { a.UserId, a.IsEnabled });

        builder.Entity<Alert>()
            .Property(a => a.Threshold)
            .HasPrecision(18, 4);

        builder.Entity<AlertTrigger>()
            .HasIndex(t => new { t.AlertId, t.TriggeredAtUtc });

        builder.Entity<UserAccount>()
            .HasKey(x => x.UserId);
    }
}
