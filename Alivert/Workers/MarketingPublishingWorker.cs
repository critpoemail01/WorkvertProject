using Alivert.Data;
using Alivert.Models;
using Microsoft.EntityFrameworkCore;

namespace Alivert.Workers;

public sealed class MarketingPublishingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MarketingPublishingWorker> _logger;

    public MarketingPublishingWorker(IServiceScopeFactory scopeFactory, ILogger<MarketingPublishingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
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
                    .Where(x => x.Status == "Scheduled" && x.ScheduledForUtc <= now)
                    .OrderBy(x => x.ScheduledForUtc)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var post in duePosts)
                {
                    post.Status = "Published";
                    post.PublishedAtUtc = now;
                }

                var dueEmails = await db.MarketingEmailSuggestions
                    .Where(x => x.Status == "Scheduled" && x.ScheduledForUtc <= now)
                    .OrderBy(x => x.ScheduledForUtc)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var email in dueEmails)
                {
                    email.Status = "Sent";
                    email.SentAtUtc = now;
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
                    _logger.LogInformation("Published {PostCount} post(s), sent {EmailCount} email sequence item(s) and opened {LandingCount} landing page(s).", duePosts.Count, dueEmails.Count, dueLandingPages.Count);
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
}
