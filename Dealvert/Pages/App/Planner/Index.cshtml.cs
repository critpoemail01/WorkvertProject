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
    private const string MvpFrequency = "Mvp14";
    private static readonly string[] SupportedFrequencies = ["Daily", "Weekdays", "ThreePerWeek", "Weekly", MvpFrequency];
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
    public IReadOnlyList<string> GoalOptions { get; } = ["leads qualificados", "marcacoes", "vendas", "lancamento", "promocao"];
    public List<PlanRow> RecentPlans { get; private set; } = new();
    public string? SuggestionMessage { get; private set; }
    public string? LeadDiscoveryMessage { get; private set; }
    public IReadOnlyList<LeadSearchQuery> LeadSearchQueries { get; private set; } = [];
    public IReadOnlyList<DiscoveredLeadEmail> DiscoveredLeadEmails { get; private set; } = [];
    public IReadOnlyList<string> LeadDiscoveryWarnings { get; private set; } = [];
    public IReadOnlyList<SectorCampaignRecommendation> CampaignRecommendations { get; private set; } = [];
    public string ActiveCampaignTemplateKey { get; private set; } = "custom";
    public CompanyLearningProfile CompanyLearning { get; private set; } = CompanyLearningProfile.Empty();
    public int CrmLeadCount { get; private set; }
    public int CrmLeadEmails { get; private set; }
    public List<CrmLeadOption> AvailableCrmLeads { get; private set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public record PlanRow(int Id, string ProductName, string Platforms, string Location, string Status, DateOnly StartDate, DateOnly EndDate, int Posts, int Emails);
    public record CrmLeadOption(int Id, string ContactName, string Email, string? CompanyName, string? Role, string? Industry, string? Country, string? City, string? Stage);

    public class PlannerInput
    {
        [Display(Name = "Nome do produto ou app")]
        [Required, StringLength(160)]
        public string ProductName { get; set; } = string.Empty;

        [Display(Name = "URL do produto")]
        [StringLength(300)]
        public string? ProductUrl { get; set; }

        [Display(Name = "Empresa ou ideia de negocio")]
        [Required, StringLength(400)]
        public string CompanyOrIdea { get; set; } = string.Empty;

        [Display(Name = "Publico-alvo")]
        [Required, StringLength(700)]
        public string TargetAudience { get; set; } = string.Empty;

        [Display(Name = "Proposta de valor")]
        [Required, StringLength(700)]
        public string ValueProposition { get; set; } = string.Empty;

        [Display(Name = "Objetivo da campanha")]
        [Required, StringLength(200)]
        public string CampaignGoal { get; set; } = "subscricoes";

        [Display(Name = "Tom")]
        [Required, StringLength(80)]
        public string Tone { get; set; } = "claro, direto e pratico";

        [Display(Name = "Plataformas")]
        public List<string> Platforms { get; set; } = ["TikTok", "Instagram", "Facebook", "LinkedIn"];

        [Display(Name = "Regiao do publico")]
        [Required, StringLength(16)]
        public string AudienceLocationScope { get; set; } = "World";

        [Display(Name = "Pais")]
        [StringLength(120)]
        public string? AudienceCountry { get; set; }

        [Display(Name = "Cidade")]
        [StringLength(160)]
        public string? AudienceCity { get; set; }

        [Display(Name = "Latitude")]
        [StringLength(32)]
        public string? AudienceLatitude { get; set; }

        [Display(Name = "Longitude")]
        [StringLength(32)]
        public string? AudienceLongitude { get; set; }

        [Display(Name = "Raio")]
        [Range(1, 1000)]
        public int AudienceRadiusKm { get; set; } = 25;

        [Display(Name = "Data de inicio")]
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Display(Name = "Duracao")]
        [Range(7, 90)]
        public int DurationDays { get; set; } = 30;

        [Display(Name = "Frequencia de publicacao")]
        [Required]
        public string Frequency { get; set; } = "Daily";

        [Display(Name = "Emails de potenciais clientes")]
        [StringLength(4000)]
        public string? EmailAudience { get; set; }

        [Display(Name = "Usar leads CRM importadas")]
        public bool UseCrmLeads { get; set; }

        [Display(Name = "Leads CRM selecionadas")]
        public List<int> SelectedCrmLeadIds { get; set; } = new();

        [Display(Name = "Filtro de leads CRM")]
        [StringLength(240)]
        public string? CrmLeadFilter { get; set; }

        [Display(Name = "Limite de leads CRM")]
        [Range(1, 500)]
        public int CrmLeadLimit { get; set; } = 100;

        [StringLength(120)]
        public string? DetectedApplicationType { get; set; }

        [StringLength(160)]
        public string SelectedTemplateKey { get; set; } = "custom";

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
                ActiveCampaignTemplateKey = template.Key;
                Input.SelectedTemplateKey = template.Key;
                SuggestionMessage = $"{template.Sector}: '{template.Title}' preparada como proxima campanha.";
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
            ModelState.AddModelError("Input.ProductUrl", "Adiciona primeiro o URL da aplicacao.");
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

            SuggestionMessage = $"Detetado: {suggestion.DetectedApplicationType}. Reve o briefing sugerido antes de gerar a campanha.";
            ModelState.Clear();
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            ModelState.Clear();
            ModelState.AddModelError("Input.ProductUrl", ex is TaskCanceledException
                ? "O URL demorou demasiado a responder."
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
            ModelState.AddModelError(string.Empty, "Template de campanha nao encontrado.");
            await LoadPlansAsync();
            return Page();
        }

        ApplyTemplate(template);
        ActiveCampaignTemplateKey = template.Key;
        Input.SelectedTemplateKey = template.Key;
        SuggestionMessage = $"{template.Sector}: '{template.Title}' aplicada. Reve a campanha estruturada antes de gerar.";
        await LoadPlansAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUseCustomAsync()
    {
        NormalizeInput();
        ModelState.Clear();

        Input.DetectedApplicationType = "Customizado";
        Input.SelectedTemplateKey = "custom";
        ActiveCampaignTemplateKey = "custom";
        SuggestionMessage = "Campanha customizada selecionada. Define objetivo, publico, oferta, canais e cadencia antes de gerar.";
        await LoadPlansAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUseMvpAsync()
    {
        NormalizeInput();
        ModelState.Clear();

        Input.DurationDays = 14;
        Input.Frequency = MvpFrequency;
        Input.Platforms = ["LinkedIn", "Instagram"];
        Input.CampaignGoal = string.IsNullOrWhiteSpace(Input.CampaignGoal) || Input.CampaignGoal is "subscriptions" or "subscricoes"
            ? "leads qualificados"
            : Input.CampaignGoal;
        Input.Tone = string.IsNullOrWhiteSpace(Input.Tone)
            ? "claro, direto e focado em conversao"
            : Input.Tone;

        SuggestionMessage = "Funil MVP aplicado: 14 dias, 5 posts LinkedIn, 5 posts Instagram, 3 emails, landing page, links UTM, aprovacao e dashboard.";
        await LoadPlansAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDiscoverLeadsAsync(CancellationToken cancellationToken)
    {
        return await SuggestLeadEmailsAsync(searchOnline: false, cancellationToken);
    }

    public async Task<IActionResult> OnPostSuggestLeadEmailsAsync(CancellationToken cancellationToken)
    {
        return await SuggestLeadEmailsAsync(searchOnline: true, cancellationToken);
    }

    private async Task<IActionResult> SuggestLeadEmailsAsync(bool searchOnline, CancellationToken cancellationToken)
    {
        NormalizeInput();
        ModelState.Clear();

        if (string.IsNullOrWhiteSpace(Input.TargetAudience) && string.IsNullOrWhiteSpace(Input.CompanyOrIdea))
        {
            ModelState.AddModelError("Input.EmailAudience", "Adiciona o publico-alvo ou ideia da empresa antes de sugerir emails.");
            await LoadPlansAsync();
            return Page();
        }

        var result = await _leadDiscovery.DiscoverAsync(new LeadDiscoveryRequest(
            string.IsNullOrWhiteSpace(Input.TargetAudience) ? Input.CompanyOrIdea : Input.TargetAudience,
            Input.CampaignGoal,
            BuildInputLocationSummary(),
            null,
            BuildLeadDiscoveryProductContext(),
            searchOnline),
            cancellationToken);

        LeadSearchQueries = result.SearchQueries;
        DiscoveredLeadEmails = result.Emails;
        LeadDiscoveryWarnings = result.Warnings;

        if (result.Emails.Count > 0)
        {
            Input.EmailAudience = MergeEmailAudience(Input.EmailAudience, result.Emails.Select(x => x.Email));
            LeadDiscoveryMessage = searchOnline
                ? $"{result.Emails.Count} email{(result.Emails.Count == 1 ? "" : "s")} publico{(result.Emails.Count == 1 ? "" : "s")} sugerido{(result.Emails.Count == 1 ? "" : "s")} a partir de paginas de empresas e adicionado{(result.Emails.Count == 1 ? "" : "s")} para revisao."
                : $"{result.Emails.Count} email{(result.Emails.Count == 1 ? "" : "s")} publico{(result.Emails.Count == 1 ? "" : "s")} encontrado{(result.Emails.Count == 1 ? "" : "s")} e adicionado{(result.Emails.Count == 1 ? "" : "s")} a lista para revisao.";
        }
        else
        {
            LeadDiscoveryMessage = searchOnline
                ? "Nao foram encontrados emails publicos automaticamente. Reve as pesquisas de prospecao ou escreve emails manualmente."
                : "Pesquisas de prospecao preparadas. Usa os resultados para validar empresas e contactos antes de enviar.";
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
            ModelState.AddModelError("Input.Platforms", "Escolhe pelo menos uma plataforma.");
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
        var crmLeadEmails = new List<string>();
        if (Input.UseCrmLeads)
        {
            if (Input.SelectedCrmLeadIds.Count == 0)
            {
                ModelState.AddModelError("Input.SelectedCrmLeadIds", "Seleciona pelo menos uma lead CRM importada para esta campanha.");
            }
            else
            {
                crmLeadEmails = await LoadSelectedCrmLeadEmailsAsync(userId, Input.SelectedCrmLeadIds);
                if (crmLeadEmails.Count == 0)
                    ModelState.AddModelError("Input.SelectedCrmLeadIds", "As leads CRM selecionadas ja nao estao disponiveis para campanhas.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadPlansAsync();
            return Page();
        }

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
            CrmLeadFilter = Input.UseCrmLeads ? "selected CRM leads" : null,
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
            ? Clip($"{draft.BusinessDna} Adaptacao aprendida: {companyLearning.RecommendedCampaignBrief}", 1000)
            : draft.BusinessDna;
        plan.BusinessDna = plan.CrmLeadSourceCount > 0
            ? Clip($"{plan.BusinessDna} Foco CRM: {plan.CrmLeadSourceCount} email{(plan.CrmLeadSourceCount == 1 ? "" : "s")} de leads importadas com filtro '{plan.CrmLeadFilter ?? "todas as leads CRM"}'.", 1000)
            : plan.BusinessDna;

        _db.MarketingPlans.Add(plan);
        await _db.SaveChangesAsync();

        return RedirectToPage("/App/Planner/Details", null, new { id = plan.Id }, "post-review");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var plan = await _db.MarketingPlans.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (plan is null)
        {
            StatusMessage = "Campanha nao encontrada ou ja eliminada.";
            return RedirectToPage();
        }

        var productName = plan.ProductName;
        _db.MarketingPlans.Remove(plan);
        await _db.SaveChangesAsync();

        StatusMessage = $"Campanha '{productName}' eliminada.";
        return RedirectToPage();
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
        CrmLeadEmails = await _db.CrmLeads.AsNoTracking().CountAsync(x =>
            x.UserId == userId &&
            x.Email != "" &&
            x.Status == "Imported" &&
            x.ConsentStatus != CrmConsentPolicy.Unsubscribed &&
            x.ConsentStatus != CrmConsentPolicy.Suppressed);
        AvailableCrmLeads = await _db.CrmLeads
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.Email != "" &&
                x.Status == "Imported" &&
                x.ConsentStatus != CrmConsentPolicy.Unsubscribed &&
                x.ConsentStatus != CrmConsentPolicy.Suppressed)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Take(80)
            .Select(x => new CrmLeadOption(
                x.Id,
                x.ContactName,
                x.Email,
                x.CompanyName,
                x.Role,
                x.Industry,
                x.Country,
                x.City,
                x.Stage))
            .ToListAsync();
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

        if (Input.Frequency.Equals(MvpFrequency, StringComparison.OrdinalIgnoreCase))
        {
            Input.Frequency = MvpFrequency;
            Input.DurationDays = 14;
            Input.Platforms = ["LinkedIn", "Instagram"];
        }

        if (!SupportedLocationScopes.Contains(Input.AudienceLocationScope, StringComparer.OrdinalIgnoreCase))
            Input.AudienceLocationScope = "World";

        Input.AudienceLocationScope = SupportedLocationScopes
            .First(scope => scope.Equals(Input.AudienceLocationScope, StringComparison.OrdinalIgnoreCase));

        Input.AudienceCountry = Clean(Input.AudienceCountry);
        Input.AudienceCity = Clean(Input.AudienceCity);
        Input.AudienceRadiusKm = Math.Clamp(Input.AudienceRadiusKm, 1, 1000);
        Input.CrmLeadFilter = Clean(Input.CrmLeadFilter);
        Input.DetectedApplicationType = Clean(Input.DetectedApplicationType);
        Input.SelectedTemplateKey = Clean(Input.SelectedTemplateKey) ?? "custom";
        ActiveCampaignTemplateKey = Input.SelectedTemplateKey;
        Input.CrmLeadLimit = Math.Clamp(Input.CrmLeadLimit, 1, 500);
        Input.SelectedCrmLeadIds = Input.SelectedCrmLeadIds
            .Where(id => id > 0)
            .Distinct()
            .Take(500)
            .ToList();

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
        SuggestionMessage = "Proxima campanha preparada com base no relatorio anterior e leads captadas.";
    }

    private async Task<List<string>> LoadSelectedCrmLeadEmailsAsync(string userId, IReadOnlyCollection<int> selectedIds)
    {
        if (selectedIds.Count == 0)
            return [];

        var query = _db.CrmLeads
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.Email != "" &&
                x.Status == "Imported" &&
                x.ConsentStatus != CrmConsentPolicy.Unsubscribed &&
                x.ConsentStatus != CrmConsentPolicy.Suppressed &&
                selectedIds.Contains(x.Id));

        return await query
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Select(x => x.Email)
            .ToListAsync();
    }

    private void ApplyTemplate(SectorCampaignRecommendation template)
    {
        Input.CampaignGoal = Clip(template.Goal, 200);
        Input.ValueProposition = Clip(string.IsNullOrWhiteSpace(Input.ValueProposition)
            ? template.Offer
            : $"{Input.ValueProposition}. Oferta da campanha: {template.Offer}", 700);
        Input.TargetAudience = Clip(string.IsNullOrWhiteSpace(Input.TargetAudience)
            ? template.Audience
            : Input.TargetAudience, 700);
        Input.CompanyOrIdea = Clip(string.IsNullOrWhiteSpace(Input.CompanyOrIdea)
            ? $"Campanha {template.Sector}: {template.Strategy}"
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

        Input.Tone = Clip($"{template.Sector.ToLowerInvariant()}, pratico e focado em conversao", 80);
        Input.DetectedApplicationType = template.Sector;
        Input.SelectedTemplateKey = template.Key;
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
            ModelState.AddModelError("Input.AudienceCountry", "Adiciona o pais do publico desta campanha.");

        if (Input.AudienceLocationScope != "City")
            return;

        if (string.IsNullOrWhiteSpace(Input.AudienceCity))
            ModelState.AddModelError("Input.AudienceCity", "Adiciona a cidade ou clica no mapa para definir o centro da campanha.");

        if (Input.AudienceRadiusKm < 1 || Input.AudienceRadiusKm > 1000)
            ModelState.AddModelError("Input.AudienceRadiusKm", "Usa um raio entre 1 e 1000 km.");
    }

    private static string BuildLocationLabel(string scope, string? country, string? city, int? radiusKm)
    {
        if (scope.Equals("Country", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(country))
            return country;

        if (scope.Equals("City", StringComparison.OrdinalIgnoreCase))
        {
            var place = string.Join(", ", new[] { city, country }.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (string.IsNullOrWhiteSpace(place))
                place = "Cidade selecionada";

            return radiusKm is > 0 ? $"{place} + {radiusKm} km" : place;
        }

        return "Mundo";
    }

    private string BuildInputLocationSummary()
    {
        return BuildLocationLabel(
            Input.AudienceLocationScope,
            Input.AudienceCountry,
            Input.AudienceCity,
            Input.AudienceRadiusKm);
    }

    private string BuildLeadDiscoveryProductContext()
    {
        return string.Join(" ", new[]
        {
            Input.ProductName,
            Input.DetectedApplicationType,
            Input.CompanyOrIdea,
            Input.ValueProposition
        }.Where(x => !string.IsNullOrWhiteSpace(x)));
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
