using Alivert.Data;
using Alivert.Models;
using Alivert.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Alivert.Pages.App.Crm;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly CrmLeadImportService _importer;

    public IndexModel(ApplicationDbContext db, UserManager<IdentityUser> userManager, CrmLeadImportService importer)
    {
        _db = db;
        _userManager = userManager;
        _importer = importer;
    }

    [BindProperty]
    public ImportInput Import { get; set; } = new();

    public List<CrmLeadRow> Leads { get; private set; } = new();
    public int TotalLeads { get; private set; }
    public int LeadsWithEmail { get; private set; }
    public int ReadyForCampaign { get; private set; }
    public int AuthorizedContacts { get; private set; }
    public int SuppressedContacts { get; private set; }
    public List<CapturedLandingLeadRow> CapturedLandingLeads { get; private set; } = new();
    public IReadOnlyList<string> ConsentStatusOptions => CrmConsentPolicy.Statuses;

    [TempData]
    public string? StatusMessage { get; set; }

    public class ImportInput
    {
        [Display(Name = "Lead source")]
        [StringLength(120)]
        public string Source { get; set; } = "CSV import";

        [Display(Name = "Consent status")]
        [StringLength(24)]
        public string ConsentStatus { get; set; } = CrmConsentPolicy.Unknown;

        [Display(Name = "Consent source")]
        [StringLength(180)]
        public string? ConsentSource { get; set; }

        [Display(Name = "Paste CRM leads")]
        [Required, StringLength(20000)]
        public string RawLeads { get; set; } = string.Empty;
    }

    public record CrmLeadRow(
        int Id,
        string ContactName,
        string Email,
        string? CompanyName,
        string? Role,
        string? Industry,
        string? Country,
        string? City,
        string? Stage,
        string? Tags,
        string? Source,
        string ConsentStatus,
        string? ConsentSource,
        DateTime UpdatedAtUtc);

    public record CapturedLandingLeadRow(
        int Id,
        string ContactName,
        string Email,
        string? CompanyName,
        string? Role,
        string? Source,
        string Campaign,
        string LandingSlug,
        bool ConsentAccepted,
        DateTime CreatedAtUtc);

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostImportLeadsAsync()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var rows = _importer.Parse(Import.RawLeads, Import.Source);
        var imported = 0;
        var updated = 0;
        var importedConsentStatus = CrmConsentPolicy.Normalize(Import.ConsentStatus);
        var importedConsentSource = Clean(Import.ConsentSource) ?? Clean(Import.Source);

        foreach (var row in rows)
        {
            var existing = await _db.CrmLeads.FirstOrDefaultAsync(x => x.UserId == userId && x.Email == row.Email);
            var isNew = existing is null;
            if (existing is null)
            {
                existing = new CrmLead
                {
                    UserId = userId,
                    Email = row.Email,
                    ContactName = row.ContactName
                };
                _db.CrmLeads.Add(existing);
                imported++;
            }
            else
            {
                updated++;
            }

            existing.ExternalId = row.ExternalId;
            existing.ContactName = row.ContactName;
            existing.Phone = row.Phone;
            existing.CompanyName = row.CompanyName;
            existing.Role = row.Role;
            existing.Industry = row.Industry;
            existing.Country = row.Country;
            existing.City = row.City;
            existing.Stage = row.Stage;
            existing.Tags = row.Tags;
            existing.Source = row.Source;
            existing.Notes = row.Notes;
            existing.Status = "Imported";
            existing.LastSyncedAtUtc = DateTime.UtcNow;

            if (isNew || importedConsentStatus != CrmConsentPolicy.Unknown)
                ApplyConsent(existing, importedConsentStatus, importedConsentSource);
        }

        var integration = await _db.CrmIntegrations
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Provider == "CSV import");
        if (integration is null)
        {
            integration = new CrmIntegration
            {
                UserId = userId,
                Provider = "CSV import",
                DisplayName = "CSV import",
                Status = "Configured"
            };
            _db.CrmIntegrations.Add(integration);
        }

        integration.LastImportedAtUtc = DateTime.UtcNow;
        integration.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        StatusMessage = $"{imported} new lead{(imported == 1 ? "" : "s")} imported and {updated} updated. These leads can now be selected in the campaign builder.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateConsentAsync(int leadId, string consentStatus)
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var lead = await _db.CrmLeads.FirstOrDefaultAsync(x => x.Id == leadId && x.UserId == userId);
        if (lead is not null)
        {
            ApplyConsent(lead, CrmConsentPolicy.Normalize(consentStatus), "Manual CRM consent hub");
            await _db.SaveChangesAsync();
            StatusMessage = $"Consent status updated for {lead.Email}.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteLeadAsync(int leadId)
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var lead = await _db.CrmLeads.FirstOrDefaultAsync(x => x.Id == leadId && x.UserId == userId);
        if (lead is not null)
        {
            _db.CrmLeads.Remove(lead);
            await _db.SaveChangesAsync();
            StatusMessage = "CRM lead removed.";
        }

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        TotalLeads = await _db.CrmLeads.AsNoTracking().CountAsync(x => x.UserId == userId);
        LeadsWithEmail = await _db.CrmLeads.AsNoTracking().CountAsync(x => x.UserId == userId && x.Email != "");
        ReadyForCampaign = await _db.CrmLeads.AsNoTracking().CountAsync(x =>
            x.UserId == userId &&
            x.Status == "Imported" &&
            x.Email != "" &&
            x.ConsentStatus != CrmConsentPolicy.Unsubscribed &&
            x.ConsentStatus != CrmConsentPolicy.Suppressed);
        AuthorizedContacts = await _db.CrmLeads.AsNoTracking().CountAsync(x =>
            x.UserId == userId &&
            x.ConsentStatus == CrmConsentPolicy.Consented);
        SuppressedContacts = await _db.CrmLeads.AsNoTracking().CountAsync(x =>
            x.UserId == userId &&
            (x.ConsentStatus == CrmConsentPolicy.Unsubscribed || x.ConsentStatus == CrmConsentPolicy.Suppressed));

        Leads = await _db.CrmLeads
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Take(200)
            .Select(x => new CrmLeadRow(
                x.Id,
                x.ContactName,
                x.Email,
                x.CompanyName,
                x.Role,
                x.Industry,
                x.Country,
                x.City,
                x.Stage,
                x.Tags,
                x.Source,
                x.ConsentStatus,
                x.ConsentSource,
                x.UpdatedAtUtc))
            .ToListAsync();

        CapturedLandingLeads = await _db.MarketingLandingLeads
            .AsNoTracking()
            .Where(x => x.MarketingLandingPage != null &&
                x.MarketingLandingPage.MarketingPlan != null &&
                x.MarketingLandingPage.MarketingPlan.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(50)
            .Select(x => new CapturedLandingLeadRow(
                x.Id,
                x.Name,
                x.Email,
                x.Company,
                x.Role,
                x.Source,
                x.MarketingLandingPage!.MarketingPlan!.ProductName,
                x.MarketingLandingPage.Slug,
                x.MarketingConsentAccepted,
                x.CreatedAtUtc))
            .ToListAsync();
    }

    private static void ApplyConsent(CrmLead lead, string consentStatus, string? consentSource)
    {
        var now = DateTime.UtcNow;
        lead.ConsentStatus = consentStatus;
        lead.ConsentSource = Clean(consentSource);

        if (consentStatus == CrmConsentPolicy.Consented)
        {
            lead.ConsentedAtUtc ??= now;
            lead.UnsubscribedAtUtc = null;
        }
        else if (CrmConsentPolicy.IsBlocked(consentStatus))
        {
            lead.UnsubscribedAtUtc ??= now;
        }
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
