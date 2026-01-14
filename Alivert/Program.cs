using Alivert.Data;
using Alivert.Services;
using Alivert.Workers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    // Optional: force /App/* to require auth even if you forget [Authorize]
    options.Conventions.AuthorizeFolder("/App");
});

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

// Redirect unauthenticated users to the home page (where we open a modal)
builder.Services.ConfigureApplicationCookie(options =>
{
    // Keep ReturnUrl so we can send the user back after login
    options.LoginPath = "/?login=1";
    options.AccessDeniedPath = "/?login=1";
    options.ReturnUrlParameter = "returnUrl";
});

// JSON auth endpoints used by the modal (instead of full-page Identity UI)
builder.Services.AddControllers();


builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 10;                 // 10 requests
        opt.Window = TimeSpan.FromMinutes(1); // per minute
        opt.QueueLimit = 0;
    });
});


// Market data + rules + notifications (MVP: fake market + DB triggers)
builder.Services.AddSingleton<IMarketDataService, FakeMarketDataService>();
builder.Services.AddSingleton<IAlertRuleEngine, AlertRuleEngine>();
builder.Services.AddScoped<IAlertDispatcher, AlertDispatcher>();
builder.Services.AddScoped<IUserAccountService, UserAccountService>();

// Background evaluator
builder.Services.AddHostedService<AlertEvaluatorWorker>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
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
