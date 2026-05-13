using Alivert.Data;
using Alivert.Models;
using Alivert.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Alivert.Pages.App.Planner;

[Authorize]
public class IndexModel : PageModel
{
    private static readonly string[] SupportedPlatforms = ["TikTok", "Instagram", "Facebook", "LinkedIn", "X", "YouTube Shorts"];
    private static readonly string[] SupportedFrequencies = ["Daily", "Weekdays", "ThreePerWeek", "Weekly"];

    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IAiMarketingPlannerService _planner;

    public IndexModel(ApplicationDbContext db, UserManager<IdentityUser> userManager, IAiMarketingPlannerService planner)
    {
        _db = db;
        _userManager = userManager;
        _planner = planner;
    }

    [BindProperty]
    public PlannerInput Input { get; set; } = new();

    public IReadOnlyList<string> PlatformOptions => SupportedPlatforms;
    public IReadOnlyList<string> FrequencyOptions => SupportedFrequencies;
    public List<PlanRow> RecentPlans { get; private set; } = new();

    public record PlanRow(int Id, string ProductName, string Platforms, string Status, DateOnly StartDate, DateOnly EndDate, int Posts, int Emails);

    public class PlannerInput
    {
        [Display(Name = "Product or app name")]
        [Required, StringLength(160)]
        public string ProductName { get; set; } = string.Empty;

        [Display(Name = "Product URL")]
        [StringLength(300)]
        public string? ProductUrl { get; set; }

        [Display(Name = "Company or business idea")]
        [Required, StringLength(400)]
        public string CompanyOrIdea { get; set; } = string.Empty;

        [Display(Name = "Target audience")]
        [Required, StringLength(700)]
        public string TargetAudience { get; set; } = string.Empty;

        [Display(Name = "Value proposition")]
        [Required, StringLength(700)]
        public string ValueProposition { get; set; } = string.Empty;

        [Display(Name = "Campaign goal")]
        [Required, StringLength(200)]
        public string CampaignGoal { get; set; } = "subscriptions";

        [Display(Name = "Tone")]
        [Required, StringLength(80)]
        public string Tone { get; set; } = "clear, direct and practical";

        [Display(Name = "Platforms")]
        public List<string> Platforms { get; set; } = ["TikTok", "Instagram", "Facebook", "LinkedIn"];

        [Display(Name = "Start date")]
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Display(Name = "Duration")]
        [Range(7, 90)]
        public int DurationDays { get; set; } = 30;

        [Display(Name = "Posting frequency")]
        [Required]
        public string Frequency { get; set; } = "Daily";

        [Display(Name = "Potential-client emails")]
        [StringLength(4000)]
        public string? EmailAudience { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadPlansAsync();
    }

    public async Task<IActionResult> OnPostGenerateAsync()
    {
        NormalizeInput();

        if (!ModelState.IsValid)
        {
            await LoadPlansAsync();
            return Page();
        }

        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var endDate = Input.StartDate.AddDays(Input.DurationDays - 1);
        var platforms = Input.Platforms
            .Where(platform => SupportedPlatforms.Contains(platform, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (platforms.Count == 0)
        {
            ModelState.AddModelError("Input.Platforms", "Choose at least one platform.");
            await LoadPlansAsync();
            return Page();
        }

        var plan = new MarketingPlan
        {
            UserId = userId,
            ProductName = Input.ProductName.Trim(),
            ProductUrl = Clean(Input.ProductUrl),
            CompanyOrIdea = Input.CompanyOrIdea.Trim(),
            TargetAudience = Input.TargetAudience.Trim(),
            ValueProposition = Input.ValueProposition.Trim(),
            CampaignGoal = Input.CampaignGoal.Trim(),
            Tone = Input.Tone.Trim(),
            Platforms = string.Join(",", platforms),
            Frequency = Input.Frequency,
            StartDate = Input.StartDate,
            EndDate = endDate,
            EmailAudience = Clean(Input.EmailAudience),
            Status = "Draft"
        };

        var draft = _planner.Generate(new AiMarketingPlanRequest(
            plan.ProductName,
            plan.ProductUrl,
            plan.CompanyOrIdea,
            plan.TargetAudience,
            plan.ValueProposition,
            plan.CampaignGoal,
            plan.Tone,
            platforms,
            plan.StartDate,
            plan.EndDate,
            plan.Frequency,
            plan.EmailAudience));

        plan.Posts.AddRange(draft.Posts);
        plan.Emails.AddRange(draft.Emails);
        plan.Leads.AddRange(draft.Leads);

        _db.MarketingPlans.Add(plan);
        await _db.SaveChangesAsync();

        return RedirectToPage("/App/Planner/Details", new { id = plan.Id });
    }

    private async Task LoadPlansAsync()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        RecentPlans = await _db.MarketingPlans
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(12)
            .Select(x => new PlanRow(
                x.Id,
                x.ProductName,
                x.Platforms,
                x.Status,
                x.StartDate,
                x.EndDate,
                x.Posts.Count,
                x.Emails.Count))
            .ToListAsync();
    }

    private void NormalizeInput()
    {
        if (!SupportedFrequencies.Contains(Input.Frequency, StringComparer.OrdinalIgnoreCase))
            Input.Frequency = "Daily";

        Input.Platforms = Input.Platforms
            .Where(platform => SupportedPlatforms.Contains(platform, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
