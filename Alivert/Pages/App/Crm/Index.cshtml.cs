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
    public IntegrationInput Integration { get; set; } = new();

    [BindProperty]
    public ImportInput Import { get; set; } = new();

    public List<CrmLeadRow> Leads { get; private set; } = new();
    public int TotalLeads { get; private set; }
    public int LeadsWithEmail { get; private set; }
    public int ReadyForCampaign { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public class IntegrationInput
    {
        [Display(Name = "CRM provider")]
        [Required, StringLength(80)]
        public string Provider { get; set; } = "CSV / manual import";

        [Display(Name = "Connection name")]
        [Required, StringLength(120)]
        public string DisplayName { get; set; } = "Prospecting CRM";

        [Display(Name = "API base URL")]
        [StringLength(500)]
        public string? ApiBaseUrl { get; set; }

        [Display(Name = "API key or private token")]
        [StringLength(200)]
        public string? ApiKey { get; set; }
    }

    public class ImportInput
    {
        [Display(Name = "Lead source")]
        [StringLength(120)]
        public string Source { get; set; } = "CRM import";

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
        DateTime UpdatedAtUtc);

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostSaveIntegrationAsync()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        ModelState.Remove("Import.RawLeads");
        ModelState.Remove("Import.Source");

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var provider = Integration.Provider.Trim();
        var integration = await _db.CrmIntegrations
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Provider == provider);

        if (integration is null)
        {
            integration = new CrmIntegration { UserId = userId, Provider = provider };
            _db.CrmIntegrations.Add(integration);
        }

        integration.DisplayName = Integration.DisplayName.Trim();
        integration.ApiBaseUrl = Clean(Integration.ApiBaseUrl);
        if (!string.IsNullOrWhiteSpace(Integration.ApiKey))
        {
            var apiKey = Integration.ApiKey.Trim();
            integration.ApiKeyHint = $"Configured ending {apiKey[Math.Max(0, apiKey.Length - 4)..]}";
        }
        integration.Status = "Configured";

        await _db.SaveChangesAsync();
        StatusMessage = "CRM integration saved. Import a list from the CRM to use it in campaign targeting.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostImportLeadsAsync()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        ModelState.Remove("Integration.Provider");
        ModelState.Remove("Integration.DisplayName");

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var rows = _importer.Parse(Import.RawLeads, Import.Source);
        var imported = 0;
        var updated = 0;

        foreach (var row in rows)
        {
            var existing = await _db.CrmLeads.FirstOrDefaultAsync(x => x.UserId == userId && x.Email == row.Email);
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
        }

        var integration = await _db.CrmIntegrations
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(x => x.UserId == userId);
        if (integration is not null)
            integration.LastImportedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        StatusMessage = $"{imported} new lead{(imported == 1 ? "" : "s")} imported and {updated} updated. These leads can now be selected in the AI Planner.";
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
        var integration = await _db.CrmIntegrations
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync();

        if (integration is not null)
        {
            Integration = new IntegrationInput
            {
                Provider = integration.Provider,
                DisplayName = integration.DisplayName,
                ApiBaseUrl = integration.ApiBaseUrl
            };
            Import.Source = integration.Provider;
        }

        TotalLeads = await _db.CrmLeads.AsNoTracking().CountAsync(x => x.UserId == userId);
        LeadsWithEmail = await _db.CrmLeads.AsNoTracking().CountAsync(x => x.UserId == userId && x.Email != "");
        ReadyForCampaign = await _db.CrmLeads.AsNoTracking().CountAsync(x => x.UserId == userId && x.Status == "Imported" && x.Email != "");

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
                x.UpdatedAtUtc))
            .ToListAsync();
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
