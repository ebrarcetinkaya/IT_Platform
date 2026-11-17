using BtPlatform.Web.Components;
using BtPlatform.Core.Security;
using BtPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using BtPlatform.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

using BtPlatform.Core.Services;
using BtPlatform.Infrastructure.Services;



var builder = WebApplication.CreateBuilder(args);



var hmacKey = builder.Configuration["Security:HmacKey"]
    ?? throw new InvalidOperationException(
        "Security:HmacKey bulunamadı. 'dotnet user-secrets set \"Security:HmacKey\" \"<anahtar>\" --project src/BtPlatform.Web' komutuyla ekleyin.");

builder.Services.AddSingleton(new HmacHasher(hmacKey));
builder.Services.AddScoped<IDirectoryService, SimulatedDirectoryService>();
builder.Services.AddHostedService<SwitchSimulationService>();
builder.Services.AddHostedService<DirectorySimulationService>();
builder.Services.AddHostedService<UpdateAgentSimulationService>();
builder.Services.AddSingleton<IInventoryCollector, PowerShellInventoryCollector>();
builder.Services.AddScoped<IRealComputerService, RealComputerService>();
builder.Services.AddSingleton<ILiveMetricsCollector, PowerShellMetricsCollector>();
builder.Services.AddSingleton<IWingetScanner, WingetScanner>();
builder.Services.AddHostedService<RealComputerMonitorService>();
builder.Services.AddHostedService<RealComputerBootstrapService>();
builder.Services.AddHostedService<RemoteCommandAgentSimulationService>();
builder.Services.AddScoped<IRemoteCommandService, RemoteCommandService>();
builder.Services.AddSingleton<IRealCommandExecutor, PowerShellCommandExecutor>();
builder.Services.AddHostedService<RealCommandRunnerService>();
builder.Services.AddSingleton<IEventCollector, PowerShellEventCollector>();
builder.Services.AddHostedService<RealEventMonitorService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/denied";
        options.Cookie.Name = "bt.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // canlıda Always olmalı
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuditLogger>();
builder.Services.AddScoped<AccessGuard>();
builder.Services.AddSingleton<LoginThrottle>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IUpdateService, UpdateService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DbSeeder.Seed(db, scope.ServiceProvider.GetRequiredService<HmacHasher>());
    NetworkSeeder.Seed(db);
    var placed = PortPlacementSeeder.Seed(db);
    if (placed > 0) app.Logger.LogInformation("{Count} bilgisayar switch portlarına yerleştirildi", placed);
    UserSeeder.Seed(db, app.Configuration);
    DirectorySeeder.Seed(db);
    SoftwareCatalogSeeder.Seed(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAccountEndpoints();

app.Run();
