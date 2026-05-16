using Dealvert.Data;
using Dealvert.Services;
using Dealvert.Workers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    // Optional: force /App/* to require auth even if you forget [Authorize]
    options.Conventions.AuthorizeFolder("/App");
})
.AddRazorRuntimeCompilation();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;

    // Basic anti-bruteforce
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
.AddEntityFrameworkStores<ApplicationDbContext>();

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.SaveTokens = true;
        });
}

// Redirect unauthenticated users to the home page (where we open a modal)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/";
    options.AccessDeniedPath = "/";
    options.ReturnUrlParameter = "returnUrl";
    options.Events.OnRedirectToLogin = context =>
    {
        var returnUrl = $"{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
        context.Response.Redirect($"/?login=1&returnUrl={Uri.EscapeDataString(returnUrl)}");
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.Redirect("/?login=1");
        return Task.CompletedTask;
    };
});

// JSON auth endpoints used by the modal (instead of full-page Identity UI)
builder.Services.AddControllers();

builder.Services.Configure<MarketDataOptions>(builder.Configuration.GetSection("MarketData"));
builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection("Notifications"));
builder.Services.Configure<PaymentOptions>(builder.Configuration.GetSection("Payments"));
builder.Services.AddHttpClient("market-data", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; Dealvert/1.0; +https://dealvert.local)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddHttpClient("notifications");
builder.Services.AddHttpClient<IUrlCampaignBriefSuggester, UrlCampaignBriefSuggester>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(12);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; DealvertUrlAnalyzer/1.0)");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
});
builder.Services.AddHttpClient<ILeadDiscoveryService, LeadDiscoveryService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(12);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; DealvertLeadDiscovery/1.0)");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 10;                 // 10 requests
        opt.Window = TimeSpan.FromMinutes(1); // per minute
        opt.QueueLimit = 0;
    });
});


// Price alert activity + delivery workflow (MVP: existing trigger engine + DB activity records)
builder.Services.AddSingleton<FakeMarketDataService>();
builder.Services.AddSingleton<IMarketDataService, MarketDataService>();
builder.Services.AddSingleton<ITechnicalIndicatorService, TechnicalIndicatorService>();
builder.Services.AddSingleton<ISymbolCatalogService, SymbolCatalogService>();
builder.Services.AddSingleton<IAlertRuleEngine, AlertRuleEngine>();
builder.Services.AddSingleton<IAiMarketingPlannerService, TemplateAiMarketingPlannerService>();
builder.Services.AddSingleton<ICampaignLibraryService, CampaignLibraryService>();
builder.Services.AddSingleton<IIntegrationAuthorizationService, IntegrationAuthorizationService>();
builder.Services.AddSingleton<ICampaignBusinessAnalyticsService, CampaignBusinessAnalyticsService>();
builder.Services.AddSingleton<CrmLeadImportService>();
builder.Services.AddScoped<ICompanyCampaignLearningService, CompanyCampaignLearningService>();
builder.Services.AddScoped<IAlertDispatcher, AlertDispatcher>();
builder.Services.AddScoped<IUserAccountService, UserAccountService>();

// Background evaluator
if (!EF.IsDesignTime)
{
    builder.Services.AddHostedService<AlertEvaluatorWorker>();
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

// Needed for AJAX antiforgery validation (modal login/register)
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();
