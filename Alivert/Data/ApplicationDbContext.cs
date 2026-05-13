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
    public DbSet<AlertDeliveryLog> AlertDeliveryLogs => Set<AlertDeliveryLog>();
    public DbSet<CreditPurchase> CreditPurchases => Set<CreditPurchase>();
    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<UserNotificationSettings> UserNotificationSettings => Set<UserNotificationSettings>();

    
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
            else if (entry.Entity is UserNotificationSettings settings)
            {
                if (entry.State == EntityState.Added)
                {
                    if (settings.CreatedAtUtc == default) settings.CreatedAtUtc = now;
                    settings.UpdatedAtUtc = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    settings.UpdatedAtUtc = now;
                }
            }
            else if (entry.Entity is AlertDeliveryLog deliveryLog)
            {
                if (entry.State == EntityState.Added && deliveryLog.CreatedAtUtc == default)
                {
                    deliveryLog.CreatedAtUtc = now;
                }
            }
            else if (entry.Entity is CreditPurchase purchase)
            {
                if (entry.State == EntityState.Added && purchase.CreatedAtUtc == default)
                {
                    purchase.CreatedAtUtc = now;
                }
            }
            else if (entry.Entity is CreditTransaction transaction)
            {
                if (entry.State == EntityState.Added && transaction.CreatedAtUtc == default)
                {
                    transaction.CreatedAtUtc = now;
                }
            }
            else if (entry.Entity is SupportTicket ticket)
            {
                if (entry.State == EntityState.Added)
                {
                    if (ticket.CreatedAtUtc == default) ticket.CreatedAtUtc = now;
                    ticket.UpdatedAtUtc = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    ticket.UpdatedAtUtc = now;
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
            .HasIndex(a => new { a.UserId, a.MarketType, a.Symbol });

        builder.Entity<Alert>()
            .Property(a => a.MarketType)
            .HasDefaultValue(MarketType.Crypto)
            .HasSentinel((MarketType)0);

        builder.Entity<Alert>()
            .HasIndex(a => new { a.UserId, a.IsEnabled });

        builder.Entity<Alert>()
            .Property(a => a.Threshold)
            .HasPrecision(18, 4);

        builder.Entity<Alert>()
            .Property(a => a.ZonePercent)
            .HasPrecision(9, 4);

        builder.Entity<Alert>()
            .Property(a => a.LastIndicatorValue)
            .HasPrecision(18, 4);

        builder.Entity<CreditPurchase>()
            .HasIndex(x => new { x.UserId, x.CreatedAtUtc });

        builder.Entity<CreditPurchase>()
            .HasIndex(x => x.ExternalReference)
            .IsUnique()
            .HasFilter("[ExternalReference] IS NOT NULL");

        builder.Entity<CreditPurchase>()
            .Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.Entity<CreditPurchase>()
            .Property(x => x.PlanCode)
            .HasDefaultValue("credits");

        builder.Entity<CreditTransaction>()
            .HasIndex(x => new { x.UserId, x.CreatedAtUtc });

        builder.Entity<CreditTransaction>()
            .HasIndex(x => x.Reference)
            .IsUnique()
            .HasFilter("[Reference] IS NOT NULL");

        builder.Entity<AlertTrigger>()
            .HasIndex(t => new { t.AlertId, t.TriggeredAtUtc });

        builder.Entity<AlertDeliveryLog>()
            .HasIndex(t => new { t.AlertId, t.CreatedAtUtc });

        builder.Entity<AlertDeliveryLog>()
            .HasIndex(t => new { t.UserId, t.CreatedAtUtc });

        builder.Entity<AlertDeliveryLog>()
            .HasIndex(t => new { t.Status, t.CreatedAtUtc });

        builder.Entity<AlertDeliveryLog>()
            .HasOne(t => t.AlertTrigger)
            .WithMany()
            .HasForeignKey(t => t.AlertTriggerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<UserAccount>()
            .HasKey(x => x.UserId);

        builder.Entity<UserNotificationSettings>()
            .HasKey(x => x.UserId);

        builder.Entity<SupportTicket>()
            .HasIndex(x => new { x.UserId, x.CreatedAtUtc });
    }
}
