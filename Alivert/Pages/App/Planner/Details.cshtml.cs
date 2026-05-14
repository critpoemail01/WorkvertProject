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
    private readonly ICampaignLibraryService _campaignLibrary;
    private readonly IIntegrationAuthorizationService _authorization;
    private readonly ICampaignBusinessAnalyticsService _businessAnalytics;
    private readonly ICompanyCampaignLearningService _companyLearning;

    public DetailsModel(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
        IUserAccountService accounts,
        ICampaignLibraryService campaignLibrary,
        IIntegrationAuthorizationService authorization,
        ICampaignBusinessAnalyticsService businessAnalytics,
        ICompanyCampaignLearningService companyLearning)
    {
        _db = db;
        _userManager = userManager;
        _accounts = accounts;
        _campaignLibrary = campaignLibrary;
        _authorization = authorization;
        _businessAnalytics = businessAnalytics;
        _companyLearning = companyLearning;
    }

    public MarketingPlan Plan { get; private set; } = default!;
    public MetricsSummary Metrics { get; private set; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0);
    public CampaignBusinessReport BusinessReport { get; private set; } = EmptyBusinessReport();
    public CompanyLearningProfile CompanyLearning { get; private set; } = CompanyLearningProfile.Empty();
    public IReadOnlyList<SectorCampaignRecommendation> NextCampaignRecommendations { get; private set; } = [];
    public IReadOnlyList<PublicationAuthorization> PublicationAuthorizations { get; private set; } = [];
    public IReadOnlyList<CampaignOperatingModule> OperatingModules { get; private set; } = [];
    public int PlanCreditUnits => CampaignCreditUsage.CountPlatformUnits(Plan.Platforms);
    [TempData]
    public string? StatusMessage { get; set; }

    public record MetricsSummary(int Drafts, int Approved, int Scheduled, int Published, int Reach, int Interactions, int Conversions, int LandingViews, int CapturedLeads);
    public record CampaignOperatingModule(string Name, string Purpose, string Status, string Detail, string Icon, bool Ready);
    private record ConsentStats(int CampaignReady, int Suppressed);

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var loaded = await LoadPlanAsync(id, tracked: true);
        if (!loaded)
        {
            StatusMessage = "Campaign not found for this account or it was already deleted.";
            return RedirectToPage("/App/Planner/Index");
        }

        if (EnsureCampaignArtifacts())
        {
            await _db.SaveChangesAsync();
            Metrics = BuildMetrics(Plan);
            BusinessReport = _businessAnalytics.BuildCampaignReport(Plan);
        }

        return Page();
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

    public async Task<IActionResult> OnPostApproveLandingAsync(int id)
    {
        var loaded = await LoadPlanAsync(id, tracked: true);
        if (!loaded) return NotFound();

        if (Plan.LandingPage is null) return NotFound();

        Plan.LandingPage.Status = "Approved";
        Plan.LandingPage.ApprovedAtUtc = DateTime.UtcNow;
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

    public async Task<IActionResult> OnPostUpdateLandingAsync(
        int id,
        string? headline,
        string? subheadline,
        string? body,
        string? primaryCallToAction,
        string? formTitle,
        string? formIntro,
        string? thankYouMessage)
    {
        var loaded = await LoadPlanAsync(id, tracked: true);
        if (!loaded) return NotFound();

        if (Plan.LandingPage is null) return NotFound();
        if (Plan.LandingPage.Status == "Published")
        {
            StatusMessage = "This landing page is already published. Create the next campaign to change the funnel.";
            return RedirectToPage(new { id });
        }

        Plan.LandingPage.Headline = RequiredText(headline, Plan.LandingPage.Headline, 180);
        Plan.LandingPage.Subheadline = RequiredText(subheadline, Plan.LandingPage.Subheadline, 260);
        Plan.LandingPage.Body = RequiredText(body, Plan.LandingPage.Body, 1600);
        Plan.LandingPage.PrimaryCallToAction = RequiredText(primaryCallToAction, Plan.LandingPage.PrimaryCallToAction, 120);
        Plan.LandingPage.FormTitle = RequiredText(formTitle, Plan.LandingPage.FormTitle, 160);
        Plan.LandingPage.FormIntro = RequiredText(formIntro, Plan.LandingPage.FormIntro, 260);
        Plan.LandingPage.ThankYouMessage = RequiredText(thankYouMessage, Plan.LandingPage.ThankYouMessage, 260);
        Plan.LandingPage.Status = "Draft";
        Plan.LandingPage.ApprovedAtUtc = null;
        Plan.Status = "Review";
        StatusMessage = "Landing page updated. Approve it before publishing.";
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

        if (Plan.LandingPage is not null && Plan.LandingPage.Status == "Draft")
        {
            Plan.LandingPage.Status = "Approved";
            Plan.LandingPage.ApprovedAtUtc = now;
        }

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

        foreach (var post in Plan.Posts.Where(x => x.Status is "Approved" or "NeedsAuthorization"))
            post.Status = "Scheduled";

        foreach (var email in Plan.Emails.Where(x => x.Status is "Approved" or "NeedsAuthorization"))
            email.Status = "Scheduled";

        if (Plan.LandingPage is not null && Plan.LandingPage.Status == "Approved")
        {
            Plan.LandingPage.Status = "Published";
            Plan.LandingPage.PublishedAtUtc = DateTime.UtcNow;
        }

        Plan.Status = "Scheduled";
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostPublishDueAsync(int id)
    {
        var loaded = await LoadPlanAsync(id, tracked: true);
        if (!loaded) return NotFound();

        var now = DateTime.UtcNow;
        var settings = await LoadSettingsAsync(Plan.UserId);
        foreach (var post in Plan.Posts.Where(x => x.Status == "Scheduled" && x.ScheduledForUtc <= now))
        {
            var authorization = _authorization.GetPostAuthorization(settings, post.Platform);
            if (authorization.IsAuthorized)
            {
                post.Status = "Published";
                post.PublishedAtUtc = now;
            }
            else
            {
                post.Status = "NeedsAuthorization";
                StatusMessage = "Some items need official publishing authorization before they can go live.";
            }
        }

        foreach (var email in Plan.Emails.Where(x => x.Status == "Scheduled" && x.ScheduledForUtc <= now))
        {
            var authorization = _authorization.GetEmailAuthorization(settings);
            if (authorization.IsAuthorized)
            {
                email.Status = "Sent";
                email.SentAtUtc = now;
            }
            else
            {
                email.Status = "NeedsAuthorization";
                StatusMessage = "Some items need official publishing authorization before they can go live.";
            }
        }

        Plan.Status = Plan.Posts.Any(x => x.Status == "NeedsAuthorization") || Plan.Emails.Any(x => x.Status == "NeedsAuthorization")
            ? "NeedsAuthorization"
            : Plan.Posts.Any(x => x.Status == "Scheduled") || Plan.Emails.Any(x => x.Status == "Scheduled")
            ? "Scheduled"
            : "Published";
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var plan = await _db.MarketingPlans.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (plan is null)
        {
            StatusMessage = "Campaign not found or already deleted.";
            return RedirectToPage("/App/Planner/Index");
        }

        var productName = plan.ProductName;
        _db.MarketingPlans.Remove(plan);
        await _db.SaveChangesAsync();

        StatusMessage = $"Campaign '{productName}' deleted.";
        return RedirectToPage("/App/Planner/Index");
    }

    private async Task<bool> LoadPlanAsync(int id, bool tracked = false)
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var query = tracked ? _db.MarketingPlans.AsQueryable() : _db.MarketingPlans.AsNoTracking();
        var plan = await query
            .Include(x => x.Posts)
            .Include(x => x.Emails)
            .Include(x => x.Leads)
            .Include(x => x.LandingPage)
            .ThenInclude(x => x!.Leads)
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
        BusinessReport = _businessAnalytics.BuildCampaignReport(plan);
        CompanyLearning = await _companyLearning.BuildAsync(plan.UserId, plan.ProductName, plan.ProductUrl, plan.CompanyOrIdea);
        var settings = await LoadSettingsAsync(plan.UserId);
        PublicationAuthorizations = BuildPublicationAuthorizations(plan, settings);
        var consentStats = await BuildConsentStatsAsync(plan.UserId);
        OperatingModules = BuildOperatingModules(plan, settings, PublicationAuthorizations, consentStats);
        NextCampaignRecommendations = _campaignLibrary.Recommend(new CampaignLibraryRequest(
            plan.ProductName,
            plan.CompanyOrIdea,
            plan.TargetAudience,
            plan.ValueProposition,
            plan.CampaignGoal,
            plan.BusinessDna), 3);
        return true;
    }

    private async Task<ConsentStats> BuildConsentStatsAsync(string userId)
    {
        var campaignReady = await _db.CrmLeads.AsNoTracking().CountAsync(x =>
            x.UserId == userId &&
            x.Status == "Imported" &&
            x.Email != "" &&
            x.ConsentStatus != CrmConsentPolicy.Unsubscribed &&
            x.ConsentStatus != CrmConsentPolicy.Suppressed);
        var suppressed = await _db.CrmLeads.AsNoTracking().CountAsync(x =>
            x.UserId == userId &&
            (x.ConsentStatus == CrmConsentPolicy.Unsubscribed || x.ConsentStatus == CrmConsentPolicy.Suppressed));

        return new ConsentStats(campaignReady, suppressed);
    }

    private async Task<UserNotificationSettings?> LoadSettingsAsync(string userId)
    {
        return await _db.UserNotificationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    private IReadOnlyList<PublicationAuthorization> BuildPublicationAuthorizations(MarketingPlan plan, UserNotificationSettings? settings)
    {
        var items = plan.Platforms
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(platform => _authorization.GetPostAuthorization(settings, platform))
            .ToList();

        if (plan.Emails.Any())
            items.Add(_authorization.GetEmailAuthorization(settings));

        items.Add(new PublicationAuthorization(true, "Landing pages", "Landing pages are published inside Promovert after campaign approval."));
        return items;
    }

    private IReadOnlyList<CampaignOperatingModule> BuildOperatingModules(
        MarketingPlan plan,
        UserNotificationSettings? settings,
        IReadOnlyList<PublicationAuthorization> publicationAuthorizations,
        ConsentStats consentStats)
    {
        var durationDays = plan.EndDate.DayNumber - plan.StartDate.DayNumber + 1;
        var authorizedChannels = publicationAuthorizations.Count(x => x.IsAuthorized);
        var totalChannels = publicationAuthorizations.Count;
        var landingStatus = plan.LandingPage is null
            ? "Missing"
            : $"{plan.LandingPage.Status}, {plan.LandingPage.Leads.Count} lead{(plan.LandingPage.Leads.Count == 1 ? "" : "s")}";
        var agencyConfigured = !string.IsNullOrWhiteSpace(settings?.AgencyName) ||
            !string.IsNullOrWhiteSpace(settings?.AgencyWorkspaceName) ||
            !string.IsNullOrWhiteSpace(settings?.AgencyBrandColor);

        return
        [
            new("Business DNA", "Analyses the URL and creates the commercial and visual profile.",
                string.IsNullOrWhiteSpace(plan.BusinessDna) ? "Needs generation" : "Ready",
                string.IsNullOrWhiteSpace(plan.ProductUrl) ? "Built from the business idea." : $"Built from {plan.ProductUrl}.",
                "bi-fingerprint", !string.IsNullOrWhiteSpace(plan.BusinessDna)),

            new("Campaign Builder", "Generates complete campaigns by objective: leads, bookings, sales, launch or promotion.",
                "Ready",
                $"{plan.CampaignGoal} over {durationDays} days for {BuildLocationLabel(plan)}.",
                "bi-diagram-3", true),

            new("Content Studio", "Creates posts, creative briefs, emails, ads and channel variations.",
                plan.Posts.Any() || plan.Emails.Any() ? "Ready" : "Empty",
                $"{plan.Posts.Count} posts/scripts, {plan.Emails.Count} emails, {plan.Posts.Count} creative briefs and ad-ready variations.",
                "bi-brush", plan.Posts.Any() || plan.Emails.Any()),

            new("Landing Page Builder", "Creates conversion pages with form, CTA and tracking.",
                plan.LandingPage is null ? "Missing" : "Ready",
                landingStatus,
                "bi-window-stack", plan.LandingPage is not null),

            new("Consent & Email Hub", "Manages authorized contacts, consent, unsubscribe and exclusion lists.",
                "Ready",
                $"{consentStats.CampaignReady} CRM contacts available, {consentStats.Suppressed} excluded from outreach.",
                "bi-shield-check", true),

            new("Approval Portal", "Lets the agency, team or client approve everything before publishing.",
                plan.Status,
                $"{Metrics.Drafts} drafts, {Metrics.Approved} approved, {Metrics.Scheduled} scheduled.",
                "bi-check2-square", true),

            new("Publishing Hub", "Schedules and publishes through official integrations.",
                authorizedChannels == totalChannels ? "Authorized" : "Needs setup",
                $"{authorizedChannels}/{totalChannels} campaign channels authorized.",
                "bi-send-check", authorizedChannels == totalChannels),

            new("Lead CRM", "Stores leads and shows origin by campaign and channel.",
                "Ready",
                $"{Metrics.CapturedLeads} captured landing leads, {plan.CrmLeadSourceCount} CRM emails used in this plan.",
                "bi-person-lines-fill", true),

            new("Analytics & Learning", "Measures results and recommends the next campaign.",
                "Ready",
                $"{BusinessReport.Leads} leads, {BusinessReport.ConversionRateLabel} conversion. Next: {BusinessReport.RecommendedNextAction}",
                "bi-graph-up-arrow", true),

            new("Agency White-label", "Supports multi-client reports, permissions and agency branding.",
                agencyConfigured ? "Configured" : "Ready to configure",
                agencyConfigured
                    ? $"{settings?.AgencyName ?? settings?.AgencyWorkspaceName ?? "Agency workspace"} branding with {PermissionModeLabel(settings?.AgencyPermissionMode)} permissions."
                    : "Configure agency name, workspace, permission mode and brand color in settings.",
                "bi-building-gear", true)
        ];
    }

    private static string PermissionModeLabel(string? mode)
    {
        return mode switch
        {
            "TeamReview" => "team review",
            "ClientApproval" => "client approval",
            _ => "owner-only"
        };
    }

    private static MetricsSummary BuildMetrics(MarketingPlan plan)
    {
        var items = plan.Posts.Select(x => x.Status).Concat(plan.Emails.Select(x => x.Status)).ToList();
        if (plan.LandingPage is not null)
            items.Add(plan.LandingPage.Status);

        var livePosts = plan.Posts.Where(x => x.Status == "Published").ToList();
        var sentEmails = plan.Emails.Where(x => x.Status == "Sent").ToList();
        var landingViews = plan.LandingPage?.ViewCount ?? 0;
        var capturedLeads = plan.LandingPage?.Leads.Count ?? 0;

        return new MetricsSummary(
            items.Count(x => x == "Draft"),
            items.Count(x => x == "Approved"),
            items.Count(x => x == "Scheduled"),
            livePosts.Count + sentEmails.Count + (plan.LandingPage?.Status == "Published" ? 1 : 0),
            livePosts.Sum(x => x.EstimatedReach) + sentEmails.Sum(x => x.EstimatedReach) + landingViews,
            livePosts.Sum(x => x.EstimatedInteractions) + sentEmails.Sum(x => x.EstimatedInteractions) + capturedLeads,
            livePosts.Sum(x => x.EstimatedConversions) + sentEmails.Sum(x => x.EstimatedConversions) + capturedLeads,
            landingViews,
            capturedLeads);
    }

    private bool EnsureCampaignArtifacts()
    {
        var changed = false;

        if (string.IsNullOrWhiteSpace(Plan.BusinessDna))
        {
            Plan.BusinessDna = Clip(
                $"Product: {Plan.ProductName}. Market: {Plan.TargetAudience}. Promise: {Plan.ValueProposition}. " +
                $"Goal: {Plan.CampaignGoal}. Tone: {Plan.Tone}. Region: {BuildLocationLabel(Plan)}. Source: {Plan.ProductUrl ?? Plan.CompanyOrIdea}.",
                1000);
            changed = true;
        }

        if (Plan.LandingPage is null)
        {
            Plan.LandingPage = new MarketingLandingPage
            {
                MarketingPlanId = Plan.Id,
                Slug = $"campaign-{Plan.Id}-{Slugify(Plan.ProductName)}",
                Headline = Clip($"{Plan.ProductName} for {Plan.TargetAudience}", 180),
                Subheadline = Clip($"A focused landing page for {Plan.CampaignGoal.ToLowerInvariant()} with {Plan.ValueProposition}.", 260),
                Body = Clip(
                    $"Business DNA: {Plan.BusinessDna}\n\n" +
                    $"This campaign connects social posts, email follow-up and a dedicated form so every visit can become a measurable lead.",
                    1600),
                PrimaryCallToAction = Clip($"Request {Plan.ProductName}", 120),
                FormTitle = Clip($"Talk to {Plan.ProductName}", 160),
                FormIntro = Clip("Leave your details and the team will follow up with the most relevant next step.", 260),
                ThankYouMessage = Clip("Thank you. Your request was captured and added to the campaign report.", 260),
                Status = "Draft"
            };
            changed = true;
        }

        return changed;
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

    private static string Slugify(string value)
    {
        var chars = value
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        var slug = string.Join("-", new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(slug) ? "campaign" : Clip(slug, 48);
    }

    private static string BuildLocationLabel(MarketingPlan plan)
    {
        if (plan.AudienceLocationScope.Equals("Country", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(plan.AudienceCountry))
            return plan.AudienceCountry;

        if (plan.AudienceLocationScope.Equals("City", StringComparison.OrdinalIgnoreCase))
        {
            var place = string.Join(", ", new[] { plan.AudienceCity, plan.AudienceCountry }.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (string.IsNullOrWhiteSpace(place))
                place = "Selected city";

            return plan.AudienceRadiusKm is > 0 ? $"{place} + {plan.AudienceRadiusKm} km" : place;
        }

        return "Worldwide";
    }

    private static CampaignBusinessReport EmptyBusinessReport()
    {
        return new CampaignBusinessReport(
            0,
            0,
            0,
            "0.0%",
            "No leads yet",
            "No channel data yet",
            0,
            "No post performance yet",
            "Publish social content with UTM links to identify the winning post.",
            "No email performance yet",
            "Send an approved email sequence to measure opens and clicks.",
            "No lead cost yet",
            "Publish approved assets and send all traffic to the landing page.",
            Array.Empty<BusinessChannelMetric>());
    }
}
