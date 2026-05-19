using Workvert.Data;
using Workvert.Models;
using Workvert.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Workvert.Pages.App.Services;

[Authorize]
public class IndexModel : PageModel
{
    private const int MaxPhotoCount = 5;
    private const long MaxPhotoBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedPhotoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private static readonly HashSet<string> AllowedPhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private readonly ApplicationDbContext _db;
    private readonly IProfessionalAdvisorService _advisor;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public IndexModel(
        ApplicationDbContext db,
        IProfessionalAdvisorService advisor,
        UserManager<IdentityUser> userManager,
        IWebHostEnvironment environment)
    {
        _db = db;
        _advisor = advisor;
        _userManager = userManager;
        _environment = environment;
    }

    [BindProperty]
    public ServiceInput Input { get; set; } = new();

    public ServiceRequestAnalysis? Analysis { get; private set; }
    public string? SavedMessage { get; private set; }
    public int SavedRequests { get; private set; }
    public int SavedProviderMatches { get; private set; }
    public DateTime? LastRequestUtc { get; private set; }
    public IReadOnlyList<string> UploadedPhotoPaths { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Input = new ServiceInput
        {
            Description = "I need someone to create a website for my company, with responsive design, basic SEO, and a contact form.",
            Location = "Portugal",
            LocationScope = "Country",
            Country = "Portugal",
            RadiusKm = 35,
            Budget = "Need a quote",
            BudgetMode = "Quote",
            BudgetCurrency = "EUR",
            Urgency = "This month",
            UrgencyPreset = "ThisMonth",
            UrgencyMonths = 3,
            RemoteAllowed = true
        };

        await LoadSavedSummaryAsync(_userManager.GetUserId(User) ?? string.Empty);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        NormalizeLocationInput();
        NormalizeBudgetInput();
        NormalizeUrgencyInput();
        ValidatePhotoInput();

        if (!ModelState.IsValid)
        {
            await LoadSavedSummaryAsync(userId);
            return Page();
        }

        Analysis = _advisor.AnalyzeServiceRequest(Input.ToRequest());
        UploadedPhotoPaths = SplitPhotoPaths(await SaveRequestAnalysisAsync(userId, Input, Analysis));
        SavedMessage = "Service request, required skills and provider matches were saved.";
        await LoadSavedSummaryAsync(userId);
        return Page();
    }

    private async Task<string?> SaveRequestAnalysisAsync(string userId, ServiceInput input, ServiceRequestAnalysis analysis)
    {
        var (budgetMin, budgetMax) = ParseBudget(input.Budget);
        var request = new ClientServiceRequest
        {
            UserId = userId,
            Title = BuildTitle(input.Description),
            Description = Trim(input.Description, 4000),
            ServiceArea = Trim(analysis.ServiceArea, 120),
            ProfessionalTypeNeeded = Trim(analysis.ProfessionalType, 120),
            Complexity = Trim(analysis.Complexity, 40),
            Location = Trim(input.Location, 180),
            RemoteAllowed = input.RemoteAllowed,
            BudgetMin = budgetMin,
            BudgetMax = budgetMax,
            Currency = DetectBudgetCurrency(input.Budget),
            Urgency = Trim(input.Urgency, 120),
            PhotoUsageNote = "Photos are used only to understand and document the requested service, not to evaluate personal characteristics.",
            RequiredSkills = Trim(string.Join(", ", analysis.RequiredSkills), 1200),
            Status = "Open"
        };

        _db.ClientServiceRequests.Add(request);
        await _db.SaveChangesAsync();

        request.PhotoPaths = await SaveUploadedPhotosAsync(input.Photos, request.Id);

        foreach (var professional in analysis.RecommendedProfessionals.Take(5))
        {
            var listing = await EnsureProviderListingAsync(professional);
            _db.ClientServiceMatches.Add(new ClientServiceMatch
            {
                ClientServiceRequestId = request.Id,
                FreelancerServiceListingId = listing.Id,
                CompatibilityScore = professional.CompatibilityScore,
                MatchReasons = Trim(string.Join(" | ", professional.MatchReasons), 2000),
                SuggestedBrief = Trim(analysis.BriefForClient, 1000),
                EstimatedBudget = Trim(professional.AveragePrice, 120),
                Status = "Suggested"
            });
        }

        await _db.SaveChangesAsync();
        return request.PhotoPaths;
    }

    private void ValidatePhotoInput()
    {
        var photos = Input.Photos
            .Where(file => file.Length > 0)
            .ToList();

        if (photos.Count > MaxPhotoCount)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(ServiceInput.Photos)}", $"Upload up to {MaxPhotoCount} photos.");
        }

        foreach (var photo in photos)
        {
            var extension = Path.GetExtension(photo.FileName);
            if (!AllowedPhotoContentTypes.Contains(photo.ContentType) || !AllowedPhotoExtensions.Contains(extension))
            {
                ModelState.AddModelError($"{nameof(Input)}.{nameof(ServiceInput.Photos)}", "Photos must be JPG, PNG or WebP images.");
                continue;
            }

            if (photo.Length > MaxPhotoBytes)
            {
                ModelState.AddModelError($"{nameof(Input)}.{nameof(ServiceInput.Photos)}", "Each photo must be 5 MB or smaller.");
            }
        }
    }

    private async Task<string?> SaveUploadedPhotosAsync(IEnumerable<IFormFile> photos, int requestId)
    {
        var validPhotos = photos
            .Where(file => file.Length > 0)
            .Take(MaxPhotoCount)
            .ToList();

        if (validPhotos.Count == 0)
        {
            return null;
        }

        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        var relativeFolder = Path.Combine("uploads", "service-requests", requestId.ToString(CultureInfo.InvariantCulture));
        var targetFolder = Path.Combine(webRoot, relativeFolder);
        Directory.CreateDirectory(targetFolder);

        var paths = new List<string>();
        foreach (var photo in validPhotos)
        {
            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var targetPath = Path.Combine(targetFolder, fileName);

            await using var stream = System.IO.File.Create(targetPath);
            await photo.CopyToAsync(stream);

            paths.Add($"/{relativeFolder.Replace(Path.DirectorySeparatorChar, '/')}/{fileName}");
        }

        return Trim(string.Join('|', paths), 2000);
    }

    private async Task<FreelancerServiceListing> EnsureProviderListingAsync(RecommendedProfessional professional)
    {
        var systemUserId = $"provider:{Slug(professional.Name)}";
        var profile = await _db.ProfessionalProfiles
            .FirstOrDefaultAsync(x => x.UserId == systemUserId);

        if (profile is null)
        {
            profile = new ProfessionalProfile
            {
                UserId = systemUserId,
                DisplayName = professional.Name,
                CurrentProfession = professional.ProfessionalArea,
                WorkMode = professional.Location.Equals("Remote", StringComparison.OrdinalIgnoreCase) ? "Remote" : "Flexible",
                EngagementType = "Service work",
                DesiredLocation = professional.Location,
                TechnicalSkills = string.Join(", ", professional.Skills),
                IsOpenToEmployment = false,
                IsOpenToFreelance = true,
                IsAvailableForServices = true
            };
            _db.ProfessionalProfiles.Add(profile);
            await _db.SaveChangesAsync();
        }
        else
        {
            profile.DisplayName = professional.Name;
            profile.CurrentProfession = professional.ProfessionalArea;
            profile.DesiredLocation = professional.Location;
            profile.TechnicalSkills = string.Join(", ", professional.Skills);
        }

        var listing = await _db.FreelancerServiceListings
            .FirstOrDefaultAsync(x => x.ProfessionalProfileId == profile.Id &&
                                      x.ServiceName == professional.ProfessionalArea &&
                                      x.Category == "Recommended provider");

        if (listing is null)
        {
            listing = new FreelancerServiceListing
            {
                ProfessionalProfileId = profile.Id,
                ServiceName = professional.ProfessionalArea,
                Category = "Recommended provider"
            };
            _db.FreelancerServiceListings.Add(listing);
        }

        listing.Description = Trim(professional.PortfolioSummary, 2500);
        listing.Skills = Trim(string.Join(", ", professional.Skills), 1200);
        listing.Currency = DetectCurrency(professional.AveragePrice);
        listing.Location = Trim(professional.Location, 180);
        listing.RemoteAvailable = professional.Location.Equals("Remote", StringComparison.OrdinalIgnoreCase);
        listing.Availability = Trim(professional.Availability, 120);
        listing.IsActive = true;

        await _db.SaveChangesAsync();
        return listing;
    }

    private async Task LoadSavedSummaryAsync(string userId)
    {
        SavedRequests = await _db.ClientServiceRequests
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .CountAsync();

        SavedProviderMatches = await _db.ClientServiceMatches
            .AsNoTracking()
            .Where(x => x.ClientServiceRequest!.UserId == userId)
            .CountAsync();

        LastRequestUtc = await _db.ClientServiceRequests
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => (DateTime?)x.CreatedAtUtc)
            .FirstOrDefaultAsync();
    }

    private static string BuildTitle(string description)
    {
        var words = Regex.Matches(description, @"[\p{L}\p{N}]+")
            .Select(x => x.Value)
            .Take(10)
            .ToList();

        return Trim(words.Count == 0 ? "Service request" : string.Join(" ", words), 180);
    }

    private static (decimal? Min, decimal? Max) ParseBudget(string? value)
    {
        var values = Regex.Matches(value ?? string.Empty, @"\d+(?:[,.]\d+)?")
            .Select(x => decimal.TryParse(x.Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : (decimal?)null)
            .Where(x => x is not null)
            .Select(x => x!.Value)
            .Take(2)
            .ToList();

        return values.Count switch
        {
            0 => (null, null),
            1 => (values[0], null),
            _ => (values.Min(), values.Max())
        };
    }

    private static string Slug(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-');
            }
        }

        return Regex.Replace(builder.ToString(), "-+", "-").Trim('-');
    }

    private static string DetectCurrency(string? value)
    {
        return (value ?? string.Empty).Contains("EUR", StringComparison.OrdinalIgnoreCase) || (value ?? string.Empty).Contains('€') ? "EUR" : "EUR";
    }

    private static string Trim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private void NormalizeLocationInput()
    {
        Input.LocationScope = NormalizeLocationScope(Input.LocationScope);
        Input.Country = Trim(Input.Country, 120);
        Input.CityOrRegion = Trim(Input.CityOrRegion, 120);
        Input.RadiusKm = Math.Clamp(Input.RadiusKm, 1, 500);

        if (Input.LocationScope is "Country" or "Region" && string.IsNullOrWhiteSpace(Input.Country))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(ServiceInput.Country)}", "Choose a country.");
        }

        if (Input.LocationScope == "Region" && string.IsNullOrWhiteSpace(Input.CityOrRegion))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(ServiceInput.CityOrRegion)}", "Choose a city or region.");
        }

        Input.Location = BuildLocationSummary(Input);
    }

    private static string NormalizeLocationScope(string? scope)
    {
        return scope?.Trim() switch
        {
            "World" => "World",
            "Country" => "Country",
            "Region" => "Region",
            _ => "Country"
        };
    }

    private static string BuildLocationSummary(ServiceInput input)
    {
        return input.LocationScope switch
        {
            "World" => "Worldwide",
            "Region" => $"{JoinLocation(input.CityOrRegion, input.Country)} within {Math.Clamp(input.RadiusKm, 1, 500)} km",
            "Country" => string.IsNullOrWhiteSpace(input.Country) ? "Country not selected" : input.Country.Trim(),
            _ => input.Location
        };
    }

    private static string JoinLocation(params string?[] parts)
    {
        var label = string.Join(", ", parts
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim()));

        return string.IsNullOrWhiteSpace(label) ? "Selected area" : label;
    }

    private static IReadOnlyList<string> SplitPhotoPaths(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private void NormalizeBudgetInput()
    {
        Input.BudgetMode = NormalizeBudgetMode(Input.BudgetMode);
        Input.BudgetCurrency = NormalizeBudgetCurrency(Input.BudgetCurrency);

        if (Input.BudgetMin is < 0)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(ServiceInput.BudgetMin)}", "Use a positive minimum budget.");
        }

        if (Input.BudgetMax is < 0)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(ServiceInput.BudgetMax)}", "Use a positive maximum budget.");
        }

        if (Input.BudgetMode == "Custom")
        {
            if (Input.BudgetMin is null && Input.BudgetMax is null)
            {
                ModelState.AddModelError($"{nameof(Input)}.{nameof(ServiceInput.BudgetMin)}", "Add at least one budget amount or choose Need a quote.");
            }

            if (Input.BudgetMin is not null && Input.BudgetMax is not null && Input.BudgetMax < Input.BudgetMin)
            {
                ModelState.AddModelError($"{nameof(Input)}.{nameof(ServiceInput.BudgetMax)}", "Maximum budget must be greater than minimum budget.");
            }
        }

        Input.Budget = BuildBudgetSummary(Input);
    }

    private static string NormalizeBudgetMode(string? mode)
    {
        return mode?.Trim() switch
        {
            "Quote" => "Quote",
            "Under300" => "Under300",
            "Standard" => "Standard",
            "Large" => "Large",
            "Custom" => "Custom",
            _ => "Standard"
        };
    }

    private static string NormalizeBudgetCurrency(string? currency)
    {
        return currency?.Trim().ToUpperInvariant() switch
        {
            "EUR" => "EUR",
            "USD" => "USD",
            "GBP" => "GBP",
            "CHF" => "CHF",
            "CAD" => "CAD",
            "BRL" => "BRL",
            _ => "EUR"
        };
    }

    private static string DetectBudgetCurrency(string? value)
    {
        var normalized = value ?? string.Empty;
        if (normalized.Contains("USD", StringComparison.OrdinalIgnoreCase)) return "USD";
        if (normalized.Contains("GBP", StringComparison.OrdinalIgnoreCase)) return "GBP";
        if (normalized.Contains("CHF", StringComparison.OrdinalIgnoreCase)) return "CHF";
        if (normalized.Contains("CAD", StringComparison.OrdinalIgnoreCase)) return "CAD";
        if (normalized.Contains("BRL", StringComparison.OrdinalIgnoreCase)) return "BRL";
        return "EUR";
    }

    private static string BuildBudgetSummary(ServiceInput input)
    {
        var currency = NormalizeBudgetCurrency(input.BudgetCurrency);
        return input.BudgetMode switch
        {
            "Quote" => "Need a quote",
            "Under300" => $"{currency} 0-300",
            "Large" => $"{currency} 1500-5000",
            "Custom" => BuildCustomBudgetSummary(currency, input.BudgetMin, input.BudgetMax),
            _ => $"{currency} 800-1500"
        };
    }

    private static string BuildCustomBudgetSummary(string currency, decimal? min, decimal? max)
    {
        if (min is not null && max is not null)
        {
            return $"{currency} {FormatBudgetAmount(min.Value)}-{FormatBudgetAmount(max.Value)}";
        }

        if (min is not null)
        {
            return $"{currency} {FormatBudgetAmount(min.Value)}+";
        }

        if (max is not null)
        {
            return $"{currency} 0-{FormatBudgetAmount(max.Value)}";
        }

        return "Need a quote";
    }

    private static string FormatBudgetAmount(decimal value)
    {
        return value % 1 == 0
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private void NormalizeUrgencyInput()
    {
        Input.UrgencyPreset = NormalizeUrgencyPreset(Input.UrgencyPreset);
        Input.UrgencyMonths = Math.Clamp(Input.UrgencyMonths, 1, 24);
        Input.Urgency = BuildUrgencySummary(Input);
    }

    private static string NormalizeUrgencyPreset(string? preset)
    {
        return preset?.Trim() switch
        {
            "Urgent" => "Urgent",
            "ThisWeek" => "ThisWeek",
            "ThisMonth" => "ThisMonth",
            "NextMonths" => "NextMonths",
            "Flexible" => "Flexible",
            _ => "ThisMonth"
        };
    }

    private static string BuildUrgencySummary(ServiceInput input)
    {
        return input.UrgencyPreset switch
        {
            "Urgent" => "Urgent",
            "ThisWeek" => "This week",
            "NextMonths" => $"Next {Math.Clamp(input.UrgencyMonths, 1, 24)} months",
            "Flexible" => "Flexible",
            _ => "This month"
        };
    }

    public class ServiceInput
    {
        [Required]
        [StringLength(1500, MinimumLength = 10)]
        [Display(Name = "Service request")]
        public string Description { get; set; } = string.Empty;

        [StringLength(180)]
        [Display(Name = "Location")]
        public string Location { get; set; } = string.Empty;

        [Display(Name = "Location scope")]
        public string LocationScope { get; set; } = "Country";

        [StringLength(120)]
        [Display(Name = "Country")]
        public string Country { get; set; } = string.Empty;

        [StringLength(120)]
        [Display(Name = "City or region")]
        public string CityOrRegion { get; set; } = string.Empty;

        [Range(1, 500)]
        [Display(Name = "Radius")]
        public int RadiusKm { get; set; } = 35;

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        [StringLength(120)]
        [Display(Name = "Approximate budget")]
        public string Budget { get; set; } = "Need a quote";

        [Display(Name = "Budget type")]
        public string BudgetMode { get; set; } = "Quote";

        [StringLength(3)]
        [Display(Name = "Currency")]
        public string BudgetCurrency { get; set; } = "EUR";

        [Range(0, 1000000)]
        [Display(Name = "Minimum")]
        public decimal? BudgetMin { get; set; }

        [Range(0, 1000000)]
        [Display(Name = "Maximum")]
        public decimal? BudgetMax { get; set; }

        [StringLength(120)]
        [Display(Name = "Urgency")]
        public string Urgency { get; set; } = string.Empty;

        [Display(Name = "Urgency")]
        public string UrgencyPreset { get; set; } = "ThisMonth";

        [Range(1, 24)]
        [Display(Name = "Months")]
        public int UrgencyMonths { get; set; } = 3;

        [Display(Name = "Reference photos")]
        public List<IFormFile> Photos { get; set; } = [];

        [Display(Name = "Can be remote")]
        public bool RemoteAllowed { get; set; } = true;

        public ServiceRequestRequest ToRequest()
        {
            return new ServiceRequestRequest(Description, Location, Budget, Urgency, RemoteAllowed);
        }
    }
}
