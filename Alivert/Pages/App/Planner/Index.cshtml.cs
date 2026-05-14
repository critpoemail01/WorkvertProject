using Alivert.Data;
using Alivert.Models;
using Alivert.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Alivert.Pages.App.Planner;

[Authorize]
public class IndexModel : PageModel
{
    private static readonly string[] SupportedPlatforms = ["TikTok", "Instagram", "Facebook", "LinkedIn", "X", "YouTube Shorts"];
    private static readonly string[] SupportedFrequencies = ["Daily", "Weekdays", "ThreePerWeek", "Weekly"];
    private static readonly string[] SupportedLocationScopes = ["World", "Country", "City"];

    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IAiMarketingPlannerService _planner;
    private readonly IUrlCampaignBriefSuggester _urlSuggester;

    public IndexModel(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
        IAiMarketingPlannerService planner,
        IUrlCampaignBriefSuggester urlSuggester)
    {
        _db = db;
        _userManager = userManager;
        _planner = planner;
        _urlSuggester = urlSuggester;
    }

    [BindProperty]
    public PlannerInput Input { get; set; } = new();

    public IReadOnlyList<string> PlatformOptions => SupportedPlatforms;
    public IReadOnlyList<string> FrequencyOptions => SupportedFrequencies;
    public IReadOnlyList<string> LocationScopeOptions => SupportedLocationScopes;
    public List<PlanRow> RecentPlans { get; private set; } = new();
    public string? SuggestionMessage { get; private set; }

    public record PlanRow(int Id, string ProductName, string Platforms, string Location, string Status, DateOnly StartDate, DateOnly EndDate, int Posts, int Emails);

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

        [Display(Name = "Audience region")]
        [Required, StringLength(16)]
        public string AudienceLocationScope { get; set; } = "World";

        [Display(Name = "Country")]
        [StringLength(120)]
        public string? AudienceCountry { get; set; }

        [Display(Name = "City")]
        [StringLength(160)]
        public string? AudienceCity { get; set; }

        [Display(Name = "Latitude")]
        [StringLength(32)]
        public string? AudienceLatitude { get; set; }

        [Display(Name = "Longitude")]
        [StringLength(32)]
        public string? AudienceLongitude { get; set; }

        [Display(Name = "Radius")]
        [Range(1, 1000)]
        public int AudienceRadiusKm { get; set; } = 25;

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

    public async Task<IActionResult> OnPostSuggestAsync(CancellationToken cancellationToken)
    {
        NormalizeInput();

        if (string.IsNullOrWhiteSpace(Input.ProductUrl))
        {
            ModelState.Clear();
            ModelState.AddModelError("Input.ProductUrl", "Add the application URL first.");
            await LoadPlansAsync();
            return Page();
        }

        try
        {
            var suggestion = await _urlSuggester.SuggestAsync(Input.ProductUrl, cancellationToken);
            Input.ProductUrl = suggestion.SourceUrl;
            Input.ProductName = suggestion.ProductName;
            Input.CompanyOrIdea = suggestion.CompanyOrIdea;
            Input.TargetAudience = suggestion.TargetAudience;
            Input.ValueProposition = suggestion.ValueProposition;
            Input.CampaignGoal = suggestion.CampaignGoal;
            Input.Tone = suggestion.Tone;
            Input.Platforms = suggestion.Platforms
                .Where(platform => SupportedPlatforms.Contains(platform, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            Input.AudienceLocationScope = "World";
            Input.AudienceCountry = null;
            Input.AudienceCity = null;
            Input.AudienceLatitude = null;
            Input.AudienceLongitude = null;
            Input.AudienceRadiusKm = 25;

            SuggestionMessage = $"Detected {suggestion.DetectedApplicationType}. Review the suggested brief before generating the plan.";
            ModelState.Clear();
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            ModelState.Clear();
            ModelState.AddModelError("Input.ProductUrl", ex is TaskCanceledException
                ? "The URL took too long to respond."
                : ex.Message);
        }

        await LoadPlansAsync();
        return Page();
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

        ValidateLocation();
        if (!ModelState.IsValid)
        {
            await LoadPlansAsync();
            return Page();
        }

        var latitude = ParseCoordinate(Input.AudienceLatitude, -90, 90);
        var longitude = ParseCoordinate(Input.AudienceLongitude, -180, 180);

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
            AudienceLocationScope = Input.AudienceLocationScope,
            AudienceCountry = Input.AudienceLocationScope is "Country" or "City" ? Clean(Input.AudienceCountry) : null,
            AudienceCity = Input.AudienceLocationScope == "City" ? Clean(Input.AudienceCity) : null,
            AudienceLatitude = Input.AudienceLocationScope == "City" ? latitude : null,
            AudienceLongitude = Input.AudienceLocationScope == "City" ? longitude : null,
            AudienceRadiusKm = Input.AudienceLocationScope == "City" ? Input.AudienceRadiusKm : null,
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
            plan.EmailAudience,
            new AiAudienceLocation(
                plan.AudienceLocationScope,
                plan.AudienceCountry,
                plan.AudienceCity,
                plan.AudienceLatitude,
                plan.AudienceLongitude,
                plan.AudienceRadiusKm)));

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
        var plans = await _db.MarketingPlans
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(12)
            .Select(x => new
            {
                x.Id,
                x.ProductName,
                x.Platforms,
                x.AudienceLocationScope,
                x.AudienceCountry,
                x.AudienceCity,
                x.AudienceRadiusKm,
                x.Status,
                x.StartDate,
                x.EndDate,
                Posts = x.Posts.Count,
                Emails = x.Emails.Count
            })
            .ToListAsync();

        RecentPlans = plans
            .Select(x => new PlanRow(
                x.Id,
                x.ProductName,
                x.Platforms,
                BuildLocationLabel(x.AudienceLocationScope, x.AudienceCountry, x.AudienceCity, x.AudienceRadiusKm),
                x.Status,
                x.StartDate,
                x.EndDate,
                x.Posts,
                x.Emails))
            .ToList();
    }

    private void NormalizeInput()
    {
        if (!SupportedFrequencies.Contains(Input.Frequency, StringComparer.OrdinalIgnoreCase))
            Input.Frequency = "Daily";

        if (!SupportedLocationScopes.Contains(Input.AudienceLocationScope, StringComparer.OrdinalIgnoreCase))
            Input.AudienceLocationScope = "World";

        Input.AudienceLocationScope = SupportedLocationScopes
            .First(scope => scope.Equals(Input.AudienceLocationScope, StringComparison.OrdinalIgnoreCase));

        Input.AudienceCountry = Clean(Input.AudienceCountry);
        Input.AudienceCity = Clean(Input.AudienceCity);
        Input.AudienceRadiusKm = Math.Clamp(Input.AudienceRadiusKm, 1, 1000);

        Input.Platforms = Input.Platforms
            .Where(platform => SupportedPlatforms.Contains(platform, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void ValidateLocation()
    {
        if (Input.AudienceLocationScope == "Country" && string.IsNullOrWhiteSpace(Input.AudienceCountry))
            ModelState.AddModelError("Input.AudienceCountry", "Add the country for this campaign audience.");

        if (Input.AudienceLocationScope != "City")
            return;

        if (string.IsNullOrWhiteSpace(Input.AudienceCity))
            ModelState.AddModelError("Input.AudienceCity", "Add the city or click the map to set the campaign center.");

        if (Input.AudienceRadiusKm < 1 || Input.AudienceRadiusKm > 1000)
            ModelState.AddModelError("Input.AudienceRadiusKm", "Use a radius between 1 and 1000 km.");
    }

    private static string BuildLocationLabel(string scope, string? country, string? city, int? radiusKm)
    {
        if (scope.Equals("Country", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(country))
            return country;

        if (scope.Equals("City", StringComparison.OrdinalIgnoreCase))
        {
            var place = string.Join(", ", new[] { city, country }.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (string.IsNullOrWhiteSpace(place))
                place = "Selected city";

            return radiusKm is > 0 ? $"{place} + {radiusKm} km" : place;
        }

        return "Worldwide";
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static double? ParseCoordinate(string? value, double min, double max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim().Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) &&
            parsed >= min &&
            parsed <= max
                ? parsed
                : null;
    }
}
