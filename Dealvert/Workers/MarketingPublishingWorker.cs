using Dealvert.Data;
using Dealvert.Models;
using Dealvert.Services;
using Microsoft.EntityFrameworkCore;

namespace Dealvert.Workers;

public sealed class MarketingPublishingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MarketingPublishingWorker> _logger;
    private readonly IIntegrationAuthorizationService _authorization;

    public MarketingPublishingWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<MarketingPublishingWorker> logger,
        IIntegrationAuthorizationService authorization)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _authorization = authorization;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var now = DateTime.UtcNow;

                var duePosts = await db.MarketingPostSuggestions
                    .Include(x => x.MarketingPlan)
                    .Where(x => x.Status == "Scheduled" && x.ScheduledForUtc <= now)
                    .OrderBy(x => x.ScheduledForUtc)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var post in duePosts)
                {
                    var settings = await LoadSettingsAsync(db, post.MarketingPlan!.UserId, stoppingToken);
                    var authorization = _authorization.GetPostAuthorization(settings, post.Platform);
                    if (authorization.IsAuthorized)
                    {
                        post.Status = "Published";
                        post.PublishedAtUtc = now;
                    }
                    else
                    {
                        post.Status = "NeedsAuthorization";
                    }
                }

                var dueEmails = await db.MarketingEmailSuggestions
                    .Include(x => x.MarketingPlan)
                    .Where(x => x.Status == "Scheduled" && x.ScheduledForUtc <= now)
                    .OrderBy(x => x.ScheduledForUtc)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var email in dueEmails)
                {
                    var settings = await LoadSettingsAsync(db, email.MarketingPlan!.UserId, stoppingToken);
                    var authorization = _authorization.GetEmailAuthorization(settings);
                    if (authorization.IsAuthorized)
                    {
                        email.Status = "Sent";
                        email.SentAtUtc = now;
                    }
                    else
                    {
                        email.Status = "NeedsAuthorization";
                    }
                }

                var scheduledPlanIds = duePosts.Select(x => x.MarketingPlanId)
                    .Concat(dueEmails.Select(x => x.MarketingPlanId))
                    .Distinct()
                    .ToList();
                var dueLandingPages = scheduledPlanIds.Count == 0
                    ? new List<MarketingLandingPage>()
                    : await db.MarketingLandingPages
                        .Where(x => scheduledPlanIds.Contains(x.MarketingPlanId) && x.Status == "Approved")
                        .ToListAsync(stoppingToken);

                foreach (var landingPage in dueLandingPages)
                {
                    landingPage.Status = "Published";
                    landingPage.PublishedAtUtc = now;
                }

                if (duePosts.Count > 0 || dueEmails.Count > 0 || dueLandingPages.Count > 0)
                {
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Processed {PostCount} post(s), {EmailCount} email sequence item(s) and opened {LandingCount} landing page(s).", duePosts.Count, dueEmails.Count, dueLandingPages.Count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not process scheduled marketing suggestions.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private static async Task<UserNotificationSettings?> LoadSettingsAsync(ApplicationDbContext db, string userId, CancellationToken ct)
    {
        return await db.UserNotificationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);
    }
}
