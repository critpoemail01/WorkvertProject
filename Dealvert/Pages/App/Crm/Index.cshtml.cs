using Dealvert.Data;
using Dealvert.Models;
using Dealvert.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Dealvert.Pages.App.Crm;

[Authorize]
public class IndexModel : PageModel
{
    private const long MaxCsvUploadBytes = 1_000_000;

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
    public CsvLeadPreview? CsvPreview { get; private set; }
    public IReadOnlyList<string> ConsentStatusOptions => CrmConsentPolicy.Statuses;

    [TempData]
    public string? StatusMessage { get; set; }

    public class ImportInput
    {
        [Display(Name = "Lead source")]
        [StringLength(120)]
        public string Source { get; set; } = string.Empty;

        [Display(Name = "Consent status")]
        [StringLength(24)]
        public string ConsentStatus { get; set; } = CrmConsentPolicy.Unknown;

        [Display(Name = "Consent source")]
        [StringLength(180)]
        public string? ConsentSource { get; set; }

        [Display(Name = "CSV file")]
        public IFormFile? CsvFile { get; set; }

        [StringLength(1000000)]
        public string? RawCsv { get; set; }

        [Display(Name = "External ID")]
        public int? ExternalIdColumn { get; set; }

        [Display(Name = "Contact name")]
        public int? ContactNameColumn { get; set; }

        [Display(Name = "Email")]
        public int? EmailColumn { get; set; }

        [Display(Name = "Phone")]
        public int? PhoneColumn { get; set; }

        [Display(Name = "Company")]
        public int? CompanyNameColumn { get; set; }

        [Display(Name = "Role")]
        public int? RoleColumn { get; set; }

        [Display(Name = "Industry")]
        public int? IndustryColumn { get; set; }

        [Display(Name = "Country")]
        public int? CountryColumn { get; set; }

        [Display(Name = "City")]
        public int? CityColumn { get; set; }

        [Display(Name = "Stage")]
        public int? StageColumn { get; set; }

        [Display(Name = "Tags")]
        public int? TagsColumn { get; set; }

        [Display(Name = "Source")]
        public int? SourceColumn { get; set; }

        [Display(Name = "Notes")]
        public int? NotesColumn { get; set; }
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

    public async Task<IActionResult> OnPostPreviewCsvAsync()
    {
        ModelState.Clear();

        if (Import.CsvFile is null || Import.CsvFile.Length == 0)
        {
            ModelState.AddModelError("Import.CsvFile", "Choose a CSV file first.");
            await LoadAsync();
            return Page();
        }

        if (Import.CsvFile.Length > MaxCsvUploadBytes)
        {
            ModelState.AddModelError("Import.CsvFile", "Use a CSV file up to 1 MB.");
            await LoadAsync();
            return Page();
        }

        Import.Source = BuildCsvSource(Import.CsvFile.FileName);
        Import.RawCsv = await ReadCsvAsync(Import.CsvFile);
        CsvPreview = _importer.Preview(Import.RawCsv);
        if (!CsvPreview.Columns.Any())
        {
            ModelState.AddModelError("Import.CsvFile", "No CSV columns were found in this file.");
            await LoadAsync();
            return Page();
        }

        ApplySuggestedMapping(CsvPreview.SuggestedMapping);
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostImportLeadsAsync()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        CsvPreview = _importer.Preview(Import.RawCsv);

        if (string.IsNullOrWhiteSpace(Import.RawCsv))
            ModelState.AddModelError("Import.CsvFile", "Choose and preview a CSV file before importing.");

        if (Import.EmailColumn is null)
            ModelState.AddModelError("Import.EmailColumn", "Match the email column before importing.");

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        Import.Source = string.IsNullOrWhiteSpace(Import.Source) ? "CSV import" : Import.Source;

        var mapping = BuildMapping();
        var rows = _importer.Parse(Import.RawCsv, Import.Source, mapping);
        if (rows.Count == 0)
        {
            ModelState.AddModelError("Import.EmailColumn", "No valid lead emails were found with this column mapping.");
            await LoadAsync();
            return Page();
        }

        var imported = 0;
        var updated = 0;
        const string importedConsentStatus = CrmConsentPolicy.Unknown;
        string? importedConsentSource = null;

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
            StatusMessage = "Source entry removed.";
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

    private static async Task<string> ReadCsvAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync();
    }

    private CrmLeadColumnMapping BuildMapping()
    {
        return new CrmLeadColumnMapping(
            Import.ExternalIdColumn,
            Import.ContactNameColumn,
            Import.EmailColumn,
            Import.PhoneColumn,
            Import.CompanyNameColumn,
            Import.RoleColumn,
            Import.IndustryColumn,
            Import.CountryColumn,
            Import.CityColumn,
            Import.StageColumn,
            Import.TagsColumn,
            null,
            Import.NotesColumn);
    }

    private void ApplySuggestedMapping(CrmLeadColumnMapping mapping)
    {
        Import.ExternalIdColumn = mapping.ExternalIdColumn;
        Import.ContactNameColumn = mapping.ContactNameColumn;
        Import.EmailColumn = mapping.EmailColumn;
        Import.PhoneColumn = mapping.PhoneColumn;
        Import.CompanyNameColumn = mapping.CompanyNameColumn;
        Import.RoleColumn = mapping.RoleColumn;
        Import.IndustryColumn = mapping.IndustryColumn;
        Import.CountryColumn = mapping.CountryColumn;
        Import.CityColumn = mapping.CityColumn;
        Import.StageColumn = mapping.StageColumn;
        Import.TagsColumn = mapping.TagsColumn;
        Import.SourceColumn = null;
        Import.NotesColumn = mapping.NotesColumn;
    }

    private static string BuildCsvSource(string? fileName)
    {
        var cleanFileName = Path.GetFileName(Clean(fileName) ?? string.Empty);
        return string.IsNullOrWhiteSpace(cleanFileName) ? "CSV import" : cleanFileName.Length > 120 ? cleanFileName[..120] : cleanFileName;
    }
}
