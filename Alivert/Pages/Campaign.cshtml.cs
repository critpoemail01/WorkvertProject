using Alivert.Data;
using Alivert.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Alivert.Pages;

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

        LandingPage.Leads.Add(new MarketingLandingLead
        {
            Name = Input.Name.Trim(),
            Email = Input.Email.Trim(),
            Phone = Clean(Input.Phone),
            Company = Clean(Input.Company),
            Role = Clean(Input.Role),
            Message = Clean(Input.Message),
            Source = SourceLabel()
        });

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
        return true;
    }

    private string SourceLabel()
    {
        var source = Request.Query["utm_source"].ToString();
        var medium = Request.Query["utm_medium"].ToString();
        var campaign = Request.Query["utm_campaign"].ToString();
        var parts = new[] { source, medium, campaign }.Where(x => !string.IsNullOrWhiteSpace(x));
        var label = string.Join(" / ", parts);
        return string.IsNullOrWhiteSpace(label) ? "Landing page form" : label;
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
