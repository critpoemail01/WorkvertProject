using Alivert.Data;
using Alivert.Models;
using Alivert.Services;
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
    private readonly IUserAccountService _accounts;

    public DetailsModel(ApplicationDbContext db, UserManager<IdentityUser> userManager, IUserAccountService accounts)
    {
        _db = db;
        _userManager = userManager;
        _accounts = accounts;
    }

    public MarketingPlan Plan { get; private set; } = default!;
    public MetricsSummary Metrics { get; private set; } = new(0, 0, 0, 0, 0, 0, 0);
    public int PlanCreditUnits => CampaignCreditUsage.CountPlatformUnits(Plan.Platforms);
    [TempData]
    public string? StatusMessage { get; set; }

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

    public async Task<IActionResult> OnPostUpdatePostAsync(
        int id,
        int postId,
        string? title,
        string? hook,
        string? caption,
        string? creativeBrief,
        string? hashtags,
        string? callToAction,
        DateTime? scheduledForLocal)
    {
        var loaded = await LoadPlanAsync(id, tracked: true);
        if (!loaded) return NotFound();

        var post = Plan.Posts.FirstOrDefault(x => x.Id == postId);
        if (post is null) return NotFound();

        if (!CanEditDraft(post.Status))
        {
            StatusMessage = "This post is already locked because it is scheduled or live.";
            return RedirectToPage(new { id });
        }

        post.Title = RequiredText(title, post.Title, 140);
        post.Hook = RequiredText(hook, post.Hook, 300);
        post.Caption = RequiredText(caption, post.Caption, 1600);
        post.CreativeBrief = RequiredText(creativeBrief, post.CreativeBrief, 900);
        post.Hashtags = OptionalText(hashtags, 300);
        post.CallToAction = RequiredText(callToAction, post.CallToAction, 180);
        ApplyLocalSchedule(scheduledForLocal, value => post.ScheduledForUtc = value);
        ResetApproval(post);
        Plan.Status = "Review";
        StatusMessage = "Post updated. Review it again before approving.";
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUpdateEmailAsync(
        int id,
        int emailId,
        string? subject,
        string? previewText,
        string? body,
        string? audienceSegment,
        DateTime? scheduledForLocal)
    {
        var loaded = await LoadPlanAsync(id, tracked: true);
        if (!loaded) return NotFound();

        var email = Plan.Emails.FirstOrDefault(x => x.Id == emailId);
        if (email is null) return NotFound();

        if (!CanEditDraft(email.Status))
        {
            StatusMessage = "This email is already locked because it is scheduled or sent.";
            return RedirectToPage(new { id });
        }

        email.Subject = RequiredText(subject, email.Subject, 160);
        email.PreviewText = RequiredText(previewText, email.PreviewText, 220);
        email.Body = RequiredText(body, email.Body, 4000);
        email.AudienceSegment = RequiredText(audienceSegment, email.AudienceSegment, 160);
        ApplyLocalSchedule(scheduledForLocal, value => email.ScheduledForUtc = value);
        ResetApproval(email);
        Plan.Status = "Review";
        StatusMessage = "Email updated. Review it again before approving.";
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUpdateLeadAsync(
        int id,
        int leadId,
        string? companyProfile,
        string? industry,
        string? contactRole,
        string? emailSearchHint,
        string? reason)
    {
        var loaded = await LoadPlanAsync(id, tracked: true);
        if (!loaded) return NotFound();

        var lead = Plan.Leads.FirstOrDefault(x => x.Id == leadId);
        if (lead is null) return NotFound();

        if (lead.Status != "Suggested" && lead.Status != "Accepted")
        {
            StatusMessage = "This lead can no longer be edited.";
            return RedirectToPage(new { id });
        }

        lead.CompanyProfile = RequiredText(companyProfile, lead.CompanyProfile, 160);
        lead.Industry = RequiredText(industry, lead.Industry, 120);
        lead.ContactRole = RequiredText(contactRole, lead.ContactRole, 120);
        lead.EmailSearchHint = RequiredText(emailSearchHint, lead.EmailSearchHint, 180);
        lead.Reason = RequiredText(reason, lead.Reason, 500);
        lead.Status = "Suggested";
        Plan.Status = "Review";
        StatusMessage = "Lead target updated. Accept it again if it is ready.";
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

        if (!CampaignCreditUsage.IsActiveMarketingPlanStatus(Plan.Status))
        {
            var userId = _userManager.GetUserId(User) ?? string.Empty;
            var requiredCredits = CampaignCreditUsage.CountPlatformUnits(Plan.Platforms);
            var limits = await _accounts.GetLimitsAsync(userId);
            if (!limits.IsUnlimited && requiredCredits > limits.RemainingSlots)
            {
                StatusMessage = $"This plan needs {requiredCredits} active platform credit{(requiredCredits == 1 ? "" : "s")}, but only {limits.RemainingSlots} remain. Remove platforms, pause another campaign or upgrade your plan.";
                return RedirectToPage(new { id });
            }
        }

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

    private static bool CanEditDraft(string status)
    {
        return status is "Draft" or "Approved";
    }

    private static void ResetApproval(MarketingPostSuggestion post)
    {
        post.Status = "Draft";
        post.ApprovedAtUtc = null;
    }

    private static void ResetApproval(MarketingEmailSuggestion email)
    {
        email.Status = "Draft";
        email.ApprovedAtUtc = null;
    }

    private static void ApplyLocalSchedule(DateTime? scheduledForLocal, Action<DateTime> apply)
    {
        if (scheduledForLocal is null)
            return;

        var local = DateTime.SpecifyKind(scheduledForLocal.Value, DateTimeKind.Local);
        apply(local.ToUniversalTime());
    }

    private static string RequiredText(string? value, string fallback, int maxLength)
    {
        var text = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return Clip(text, maxLength);
    }

    private static string? OptionalText(string? value, int maxLength)
    {
        return string.IsNullOrWhiteSpace(value) ? null : Clip(value.Trim(), maxLength);
    }

    private static string Clip(string value, int maxLength)
    {
        if (value.Length <= maxLength)
            return value;

        return value[..Math.Max(0, maxLength - 3)].TrimEnd() + "...";
    }
}
