using Dealvert.Data;
using Dealvert.Models;
using Dealvert.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Dealvert.Pages;

public class CampaignModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public CampaignModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public MarketingLandingPage LandingPage { get; private set; } = default!;
    public MarketingPlan Plan { get; private set; } = default!;
    public bool Submitted { get; private set; }
    public string? GoogleAnalyticsMeasurementId { get; private set; }
    public string? MetaPixelId { get; private set; }
    public string? AgencyName { get; private set; }
    public string? AgencyBrandColor { get; private set; }
    public string? AgencyReportFooter { get; private set; }
    public string ConsentText => $"I agree to be contacted about this Dealvert product watch for {Plan.ProductName} and understand I can unsubscribe at any time.";

    [BindProperty]
    public LeadInput Input { get; set; } = new();

    public class LeadInput
    {
        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(80)]
        public string? Phone { get; set; }

        [StringLength(160)]
        public string? Company { get; set; }

        [StringLength(120)]
        public string? Role { get; set; }

        [StringLength(800)]
        public string? Message { get; set; }

        [Display(Name = "Deal watch consent")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "Confirm consent before submitting the notification request.")]
        public bool MarketingConsentAccepted { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var loaded = await LoadAsync(slug, tracked: true);
        if (!loaded)
            return NotFound();

        LandingPage.ViewCount++;
        await _db.SaveChangesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug)
    {
        var loaded = await LoadAsync(slug, tracked: true);
        if (!loaded)
            return NotFound();

        if (!ModelState.IsValid)
            return Page();

        var now = DateTime.UtcNow;
        var email = Input.Email.Trim();
        var source = SourceLabel();
        var consentText = ConsentText;

        LandingPage.Leads.Add(new MarketingLandingLead
        {
            Name = Input.Name.Trim(),
            Email = email,
            Phone = Clean(Input.Phone),
            Company = Clean(Input.Company),
            Role = Clean(Input.Role),
            Message = Clean(Input.Message),
            Source = source,
            MarketingConsentAccepted = Input.MarketingConsentAccepted,
            ConsentText = consentText,
            ConsentedAtUtc = now
        });

        await UpsertCapturedLeadAsync(email, source, consentText, now);
        await _db.SaveChangesAsync();
        Submitted = true;
        ModelState.Clear();
        Input = new LeadInput();
        return Page();
    }

    private async Task<bool> LoadAsync(string slug, bool tracked)
    {
        var query = tracked ? _db.MarketingLandingPages.AsQueryable() : _db.MarketingLandingPages.AsNoTracking();
        var landing = await query
            .Include(x => x.MarketingPlan)
            .Include(x => x.Leads)
            .FirstOrDefaultAsync(x => x.Slug == slug && x.Status == "Published");

        if (landing?.MarketingPlan is null)
            return false;

        LandingPage = landing;
        Plan = landing.MarketingPlan;

        var settings = await _db.UserNotificationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == Plan.UserId);
        GoogleAnalyticsMeasurementId = settings?.GoogleAnalyticsMeasurementId;
        MetaPixelId = settings?.MetaPixelId;
        AgencyName = settings?.AgencyName;
        AgencyBrandColor = settings?.AgencyBrandColor;
        AgencyReportFooter = settings?.AgencyReportFooter;
        return true;
    }

    private async Task UpsertCapturedLeadAsync(string email, string source, string consentText, DateTime now)
    {
        var crmLead = await _db.CrmLeads
            .FirstOrDefaultAsync(x => x.UserId == Plan.UserId && x.Email == email);

        if (crmLead is null)
        {
            crmLead = new CrmLead
            {
                UserId = Plan.UserId,
                Email = email
            };
            _db.CrmLeads.Add(crmLead);
        }

        crmLead.ContactName = Clip(Input.Name.Trim(), 160);
        crmLead.Phone = Clean(Input.Phone);
        crmLead.CompanyName = Clean(Input.Company);
        crmLead.Role = Clean(Input.Role);
        crmLead.Stage = "Deal watch contact";
        crmLead.Tags = Clip($"deal-watch,{Plan.ProductName}", 300);
        crmLead.Source = Clip($"Deal watch page: {Plan.ProductName} / {source}", 120);
        crmLead.Notes = Clean(Input.Message);
        crmLead.Status = "Imported";
        crmLead.LastSyncedAtUtc = now;

        if (!string.Equals(crmLead.ConsentStatus, CrmConsentPolicy.Suppressed, StringComparison.OrdinalIgnoreCase))
        {
            crmLead.ConsentStatus = CrmConsentPolicy.Consented;
            crmLead.ConsentSource = Clip($"Deal watch form: {LandingPage.Slug}", 180);
            crmLead.ConsentedAtUtc = now;
            crmLead.UnsubscribedAtUtc = null;
        }
    }

    private string SourceLabel()
    {
        var source = Request.Query["utm_source"].ToString();
        var medium = Request.Query["utm_medium"].ToString();
        var sourceTag = Request.Query["utm_campaign"].ToString();
        var parts = new[] { source, medium, sourceTag }.Where(x => !string.IsNullOrWhiteSpace(x));
        var label = string.Join(" / ", parts);
        return string.IsNullOrWhiteSpace(label) ? "Deal watch page" : label;
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string Clip(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength].TrimEnd();
    }
}
