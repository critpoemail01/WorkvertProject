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
    private readonly ILeadDiscoveryService _leadDiscovery;
    private readonly ICampaignLibraryService _campaignLibrary;
    private readonly ICompanyCampaignLearningService _companyLearning;

    public IndexModel(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
        IAiMarketingPlannerService planner,
        IUrlCampaignBriefSuggester urlSuggester,
        ILeadDiscoveryService leadDiscovery,
        ICampaignLibraryService campaignLibrary,
        ICompanyCampaignLearningService companyLearning)
    {
        _db = db;
        _userManager = userManager;
        _planner = planner;
        _urlSuggester = urlSuggester;
        _leadDiscovery = leadDiscovery;
        _campaignLibrary = campaignLibrary;
        _companyLearning = companyLearning;
    }

    [BindProperty]
    public PlannerInput Input { get; set; } = new();

    public IReadOnlyList<string> PlatformOptions => SupportedPlatforms;
    public IReadOnlyList<string> FrequencyOptions => SupportedFrequencies;
    public IReadOnlyList<string> LocationScopeOptions => SupportedLocationScopes;
    public List<PlanRow> RecentPlans { get; private set; } = new();
    public string? SuggestionMessage { get; private set; }
    public string? LeadDiscoveryMessage { get; private set; }
    public IReadOnlyList<LeadSearchQuery> LeadSearchQueries { get; private set; } = [];
    public IReadOnlyList<DiscoveredLeadEmail> DiscoveredLeadEmails { get; private set; } = [];
    public IReadOnlyList<string> LeadDiscoveryWarnings { get; private set; } = [];
    public IReadOnlyList<SectorCampaignRecommendation> CampaignRecommendations { get; private set; } = [];
    public CompanyLearningProfile CompanyLearning { get; private set; } = CompanyLearningProfile.Empty();
    public int CrmLeadCount { get; private set; }
    public int CrmLeadEmails { get; private set; }

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

        [Display(Name = "Use imported CRM leads")]
        public bool UseCrmLeads { get; set; }

        [Display(Name = "CRM lead filter")]
        [StringLength(240)]
        public string? CrmLeadFilter { get; set; }

        [Display(Name = "CRM lead limit")]
        [Range(1, 500)]
        public int CrmLeadLimit { get; set; } = 100;

        [StringLength(120)]
        public string? DetectedApplicationType { get; set; }

        [Display(Name = "Company websites to scan")]
        [StringLength(3000)]
        public string? LeadCompanyUrls { get; set; }
    }

    public async Task OnGetAsync(int? sourcePlanId = null, string? templateKey = null)
    {
        if (sourcePlanId is not null)
            await LoadNextCampaignSourceAsync(sourcePlanId.Value);

        if (!string.IsNullOrWhiteSpace(templateKey))
        {
            var template = _campaignLibrary.Find(templateKey);
            if (template is not null)
            {
                ApplyTemplate(template);
                SuggestionMessage = $"{template.Sector}: '{template.Title}' prepared as the next campaign.";
            }
        }

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
            Input.DetectedApplicationType = suggestion.DetectedApplicationType;
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

    public async Task<IActionResult> OnPostUseTemplateAsync(string templateKey)
    {
        NormalizeInput();
        ModelState.Clear();

        var template = _campaignLibrary.Find(templateKey);
        if (template is null)
        {
            ModelState.AddModelError(string.Empty, "Campaign template not found.");
            await LoadPlansAsync();
            return Page();
        }

        ApplyTemplate(template);
        SuggestionMessage = $"{template.Sector}: '{template.Title}' applied. Review the structured campaign before generating the plan.";
        await LoadPlansAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDiscoverLeadsAsync(CancellationToken cancellationToken)
    {
        NormalizeInput();
        ModelState.Clear();

        if (string.IsNullOrWhiteSpace(Input.TargetAudience))
        {
            ModelState.AddModelError("Input.TargetAudience", "Add the target audience before discovering potential clients.");
            await LoadPlansAsync();
            return Page();
        }

        var result = await _leadDiscovery.DiscoverAsync(new LeadDiscoveryRequest(
            Input.TargetAudience,
            Input.CampaignGoal,
            BuildInputLocationSummary(),
            Input.LeadCompanyUrls),
            cancellationToken);

        LeadSearchQueries = result.SearchQueries;
        DiscoveredLeadEmails = result.Emails;
        LeadDiscoveryWarnings = result.Warnings;

        if (result.Emails.Count > 0)
        {
            Input.EmailAudience = MergeEmailAudience(Input.EmailAudience, result.Emails.Select(x => x.Email));
            LeadDiscoveryMessage = $"{result.Emails.Count} public email address{(result.Emails.Count == 1 ? "" : "es")} found and added to the potential-client list for review.";
        }
        else
        {
            LeadDiscoveryMessage = "Prospecting searches were prepared. Add company websites or contact-page URLs to extract public emails.";
        }

        ModelState.Clear();
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
        var crmLeadEmails = Input.UseCrmLeads
            ? await LoadMatchingCrmLeadEmailsAsync(userId, Input.CrmLeadFilter, Input.CrmLeadLimit)
            : [];
        var emailAudience = Input.UseCrmLeads && crmLeadEmails.Count > 0
            ? MergeEmailAudience(Input.EmailAudience, crmLeadEmails)
            : Input.EmailAudience;
        var companyLearning = await _companyLearning.BuildAsync(
            userId,
            Input.ProductName,
            Input.ProductUrl,
            Input.CompanyOrIdea);

        var plan = new MarketingPlan
        {
            UserId = userId,
            ProductName = Input.ProductName.Trim(),
            ProductUrl = Clean(Input.ProductUrl),
            CompanyOrIdea = Input.CompanyOrIdea.Trim(),
            BusinessDna = string.Empty,
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
            EmailAudience = Clean(emailAudience),
            CrmLeadFilter = Input.UseCrmLeads ? Clean(Input.CrmLeadFilter) : null,
            CrmLeadSourceCount = crmLeadEmails.Count,
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
                plan.AudienceRadiusKm),
            companyLearning));

        plan.Posts.AddRange(draft.Posts);
        plan.Emails.AddRange(draft.Emails);
        plan.Leads.AddRange(draft.Leads);
        plan.LandingPage = draft.LandingPage;
        plan.BusinessDna = companyLearning.HasData
            ? Clip($"{draft.BusinessDna} Learned adaptation: {companyLearning.RecommendedCampaignBrief}", 1000)
            : draft.BusinessDna;
        plan.BusinessDna = plan.CrmLeadSourceCount > 0
            ? Clip($"{plan.BusinessDna} CRM focus: {plan.CrmLeadSourceCount} imported lead email{(plan.CrmLeadSourceCount == 1 ? "" : "s")} matching '{plan.CrmLeadFilter ?? "all CRM leads"}'.", 1000)
            : plan.BusinessDna;

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

        CrmLeadCount = await _db.CrmLeads.AsNoTracking().CountAsync(x => x.UserId == userId);
        CrmLeadEmails = await _db.CrmLeads.AsNoTracking().CountAsync(x => x.UserId == userId && x.Email != "");
        CompanyLearning = await _companyLearning.BuildAsync(
            userId,
            Input.ProductName,
            Input.ProductUrl,
            Input.CompanyOrIdea);
        CampaignRecommendations = _campaignLibrary.Recommend(BuildCampaignLibraryRequest(), 3);
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
        Input.CrmLeadFilter = Clean(Input.CrmLeadFilter);
        Input.DetectedApplicationType = Clean(Input.DetectedApplicationType);
        Input.CrmLeadLimit = Math.Clamp(Input.CrmLeadLimit, 1, 500);

        Input.Platforms = Input.Platforms
            .Where(platform => SupportedPlatforms.Contains(platform, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task LoadNextCampaignSourceAsync(int sourcePlanId)
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var source = await _db.MarketingPlans
            .AsNoTracking()
            .Include(x => x.LandingPage)
            .ThenInclude(x => x!.Leads)
            .FirstOrDefaultAsync(x => x.Id == sourcePlanId && x.UserId == userId);

        if (source is null)
            return;

        Input.ProductName = source.ProductName;
        Input.ProductUrl = source.ProductUrl;
        Input.CompanyOrIdea = source.CompanyOrIdea;
        Input.TargetAudience = source.TargetAudience;
        Input.ValueProposition = source.ValueProposition;
        Input.CampaignGoal = source.CampaignGoal;
        Input.Tone = source.Tone;
        Input.Platforms = source.Platforms
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(platform => SupportedPlatforms.Contains(platform, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        Input.AudienceLocationScope = source.AudienceLocationScope;
        Input.AudienceCountry = source.AudienceCountry;
        Input.AudienceCity = source.AudienceCity;
        Input.AudienceLatitude = source.AudienceLatitude?.ToString(CultureInfo.InvariantCulture);
        Input.AudienceLongitude = source.AudienceLongitude?.ToString(CultureInfo.InvariantCulture);
        Input.AudienceRadiusKm = source.AudienceRadiusKm ?? 25;
        Input.StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        Input.DurationDays = Math.Clamp(source.EndDate.DayNumber - source.StartDate.DayNumber + 1, 7, 90);
        Input.Frequency = source.Frequency;
        Input.EmailAudience = MergeEmailAudience(
            source.EmailAudience,
            source.LandingPage?.Leads.Select(x => x.Email) ?? Enumerable.Empty<string>());
        Input.UseCrmLeads = source.CrmLeadSourceCount > 0;
        Input.CrmLeadFilter = source.CrmLeadFilter;
        Input.CrmLeadLimit = Math.Max(100, source.CrmLeadSourceCount);
        SuggestionMessage = "Next campaign prepared from the previous campaign report and captured leads.";
    }

    private async Task<List<string>> LoadMatchingCrmLeadEmailsAsync(string userId, string? filter, int limit)
    {
        var query = _db.CrmLeads
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Email != "" && x.Status == "Imported");

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var term = filter.Trim();
            query = query.Where(x =>
                (x.CompanyName != null && x.CompanyName.Contains(term)) ||
                (x.ContactName != null && x.ContactName.Contains(term)) ||
                (x.Role != null && x.Role.Contains(term)) ||
                (x.Industry != null && x.Industry.Contains(term)) ||
                (x.Country != null && x.Country.Contains(term)) ||
                (x.City != null && x.City.Contains(term)) ||
                (x.Stage != null && x.Stage.Contains(term)) ||
                (x.Tags != null && x.Tags.Contains(term)));
        }

        return await query
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Take(limit)
            .Select(x => x.Email)
            .ToListAsync();
    }

    private void ApplyTemplate(SectorCampaignRecommendation template)
    {
        Input.CampaignGoal = Clip(template.Goal, 200);
        Input.ValueProposition = Clip(string.IsNullOrWhiteSpace(Input.ValueProposition)
            ? template.Offer
            : $"{Input.ValueProposition}. Campaign offer: {template.Offer}", 700);
        Input.TargetAudience = Clip(string.IsNullOrWhiteSpace(Input.TargetAudience)
            ? template.Audience
            : Input.TargetAudience, 700);
        Input.CompanyOrIdea = Clip(string.IsNullOrWhiteSpace(Input.CompanyOrIdea)
            ? $"{template.Sector} campaign: {template.Strategy}"
            : Input.CompanyOrIdea, 400);
        Input.DurationDays = Math.Clamp(template.DurationDays, 7, 90);
        Input.Frequency = SupportedFrequencies.Contains(template.Frequency, StringComparer.OrdinalIgnoreCase)
            ? template.Frequency
            : "ThreePerWeek";
        Input.Platforms = template.Platforms
            .Where(platform => SupportedPlatforms.Contains(platform, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (Input.Platforms.Count == 0)
            Input.Platforms = ["Instagram", "Facebook", "LinkedIn"];

        Input.Tone = Clip($"{template.Sector.ToLowerInvariant()}, practical and conversion-focused", 80);
        Input.DetectedApplicationType = template.Sector;
    }

    private CampaignLibraryRequest BuildCampaignLibraryRequest()
    {
        return new CampaignLibraryRequest(
            Input.ProductName,
            Input.CompanyOrIdea,
            Input.TargetAudience,
            Input.ValueProposition,
            Input.CampaignGoal,
            Input.DetectedApplicationType);
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

    private string BuildInputLocationSummary()
    {
        return BuildLocationLabel(
            Input.AudienceLocationScope,
            Input.AudienceCountry,
            Input.AudienceCity,
            Input.AudienceRadiusKm);
    }

    private static string MergeEmailAudience(string? existingAudience, IEnumerable<string> discoveredEmails)
    {
        var emails = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var totalLength = 0;

        foreach (var value in SplitAudience(existingAudience).Concat(discoveredEmails))
        {
            var cleaned = value.Trim();
            if (!cleaned.Contains('@') || !seen.Add(cleaned))
                continue;

            var projectedLength = totalLength + cleaned.Length + (emails.Count == 0 ? 0 : Environment.NewLine.Length);
            if (projectedLength > 4000)
                break;

            emails.Add(cleaned);
            totalLength = projectedLength;
        }

        return string.Join(Environment.NewLine, emails);
    }

    private static IEnumerable<string> SplitAudience(string? audience)
    {
        return string.IsNullOrWhiteSpace(audience)
            ? []
            : audience.Split(new[] { '\r', '\n', ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string Clip(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..Math.Max(0, maxLength - 3)].TrimEnd() + "...";
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
