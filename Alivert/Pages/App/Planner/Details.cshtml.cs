using Alivert.Data;
using Alivert.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Alivert.Pages.App.Planner;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public DetailsModel(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public MarketingPlan Plan { get; private set; } = default!;
    public MetricsSummary Metrics { get; private set; } = new(0, 0, 0, 0, 0, 0, 0);
    public string? StatusMessage { get; private set; }

    public record MetricsSummary(int Drafts, int Approved, int Scheduled, int Published, int Reach, int Interactions, int Conversions);

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var loaded = await LoadPlanAsync(id);
        return loaded ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostApprovePostAsync(int id, int postId)
    {
        var loaded = await LoadPlanAsync(id, tracked: true);
        if (!loaded) return NotFound();

        var post = Plan.Posts.FirstOrDefault(x => x.Id == postId);
        if (post is null) return NotFound();

        post.Status = "Approved";
        post.ApprovedAtUtc = DateTime.UtcNow;
        Plan.Status = "Review";
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostApproveEmailAsync(int id, int emailId)
    {
        var loaded = await LoadPlanAsync(id, tracked: true);
        if (!loaded) return NotFound();

        var email = Plan.Emails.FirstOrDefault(x => x.Id == emailId);
        if (email is null) return NotFound();

        email.Status = "Approved";
        email.ApprovedAtUtc = DateTime.UtcNow;
        Plan.Status = "Review";
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostApproveLeadAsync(int id, int leadId)
    {
        var loaded = await LoadPlanAsync(id, tracked: true);
        if (!loaded) return NotFound();

        var lead = Plan.Leads.FirstOrDefault(x => x.Id == leadId);
        if (lead is null) return NotFound();

        lead.Status = "Accepted";
        Plan.Status = "Review";
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostApproveAllAsync(int id)
    {
        var loaded = await LoadPlanAsync(id, tracked: true);
        if (!loaded) return NotFound();

        var now = DateTime.UtcNow;
        foreach (var post in Plan.Posts.Where(x => x.Status == "Draft"))
        {
            post.Status = "Approved";
            post.ApprovedAtUtc = now;
        }

        foreach (var email in Plan.Emails.Where(x => x.Status == "Draft"))
        {
            email.Status = "Approved";
            email.ApprovedAtUtc = now;
        }

        foreach (var lead in Plan.Leads.Where(x => x.Status == "Suggested"))
            lead.Status = "Accepted";

        Plan.Status = "Approved";
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostScheduleApprovedAsync(int id)
    {
        var loaded = await LoadPlanAsync(id, tracked: true);
        if (!loaded) return NotFound();

        foreach (var post in Plan.Posts.Where(x => x.Status == "Approved"))
            post.Status = "Scheduled";

        foreach (var email in Plan.Emails.Where(x => x.Status == "Approved"))
            email.Status = "Scheduled";

        Plan.Status = "Scheduled";
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostPublishDueAsync(int id)
    {
        var loaded = await LoadPlanAsync(id, tracked: true);
        if (!loaded) return NotFound();

        var now = DateTime.UtcNow;
        foreach (var post in Plan.Posts.Where(x => x.Status == "Scheduled" && x.ScheduledForUtc <= now))
        {
            post.Status = "Published";
            post.PublishedAtUtc = now;
        }

        foreach (var email in Plan.Emails.Where(x => x.Status == "Scheduled" && x.ScheduledForUtc <= now))
        {
            email.Status = "Sent";
            email.SentAtUtc = now;
        }

        Plan.Status = Plan.Posts.Any(x => x.Status == "Scheduled") || Plan.Emails.Any(x => x.Status == "Scheduled")
            ? "Scheduled"
            : "Published";
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadPlanAsync(int id, bool tracked = false)
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var query = tracked ? _db.MarketingPlans.AsQueryable() : _db.MarketingPlans.AsNoTracking();
        var plan = await query
            .Include(x => x.Posts)
            .Include(x => x.Emails)
            .Include(x => x.Leads)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (plan is null)
            return false;

        plan.Posts = plan.Posts
            .OrderBy(x => x.ScheduledForUtc)
            .ThenBy(x => x.Platform)
            .ToList();
        plan.Emails = plan.Emails
            .OrderBy(x => x.ScheduledForUtc)
            .ToList();
        plan.Leads = plan.Leads
            .OrderBy(x => x.Industry)
            .ToList();

        Plan = plan;
        Metrics = BuildMetrics(plan);
        return true;
    }

    private static MetricsSummary BuildMetrics(MarketingPlan plan)
    {
        var items = plan.Posts.Select(x => x.Status).Concat(plan.Emails.Select(x => x.Status)).ToList();
        var livePosts = plan.Posts.Where(x => x.Status == "Published").ToList();
        var sentEmails = plan.Emails.Where(x => x.Status == "Sent").ToList();

        return new MetricsSummary(
            items.Count(x => x == "Draft"),
            items.Count(x => x == "Approved"),
            items.Count(x => x == "Scheduled"),
            livePosts.Count + sentEmails.Count,
            livePosts.Sum(x => x.EstimatedReach) + sentEmails.Sum(x => x.EstimatedReach),
            livePosts.Sum(x => x.EstimatedInteractions) + sentEmails.Sum(x => x.EstimatedInteractions),
            livePosts.Sum(x => x.EstimatedConversions) + sentEmails.Sum(x => x.EstimatedConversions));
    }
}
