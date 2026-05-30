// Program.cs registers all application services and middleware — high coupling is
// inherent to the composition root pattern and not a refactoring candidate.
#pragma warning disable CA1506
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Orleans.Configuration;
using Spx.Account.Application;
using Spx.Data;
using Spx.Game.Application;
using Spx.Web.Adapters;
using Spx.Web.Adapters.Account;
using Spx.Web.Components;
using Spx.Web.Endpoints;
using Spx.Web.Hubs;
using Spx.Web.Options;

var builder = WebApplication.CreateBuilder(args);
var orleansClusterId = builder.Configuration["Orleans:ClusterId"] ?? "spx-local-cluster";
var orleansServiceId = builder.Configuration["Orleans:ServiceId"] ?? "spx-local-service";

builder.AddServiceDefaults();
builder.AddKeyedRedisClient("orleans-redis");
builder.AddNpgsqlDbContext<ApplicationDbContext>("appdb");
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("appdb")
        ?? builder.Configuration["APPDB_URI"]
        ?? throw new InvalidOperationException("The appdb connection string was not configured.");

    options.UseNpgsql(connectionString);
});
builder.UseOrleansClient(clientBuilder =>
{
    clientBuilder.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = orleansClusterId;
        options.ServiceId = orleansServiceId;
    });
    clientBuilder.UseRedisClustering(
        builder.Configuration.GetConnectionString("orleans-redis")
            ?? throw new InvalidOperationException(
                "Connection string 'orleans-redis' was not configured."
            )
    );
});
builder.Services.Configure<AppUrlOptions>(
    builder.Configuration.GetSection(AppUrlOptions.SectionName)
);
builder.Services.Configure<ResendOptions>(
    builder.Configuration.GetSection(ResendOptions.SectionName)
);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder
    .Services.AddSignalR()
    .AddStackExchangeRedis(
        builder.Configuration.GetConnectionString("orleans-redis")
            ?? throw new InvalidOperationException(
                "The orleans-redis connection string was not configured."
            )
    );
builder.Services.AddSingleton<NexusInvalidationHubBridge>();
builder.Services.AddSingleton<IGameInvalidationHubBridge>(sp =>
    sp.GetRequiredService<NexusInvalidationHubBridge>()
);
builder.Services.AddHostedService(sp => sp.GetRequiredService<NexusInvalidationHubBridge>());
builder.Services.AddScoped<INexusHubAccessService, NexusHubAccessService>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/account/logout";
    options.AccessDeniedPath = "/not-authorized";
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddAuthorization();
builder
    .Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.SignIn.RequireConfirmedEmail = true;
        options.User.RequireUniqueEmail = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IAccountEmailSender, LoggingAccountEmailSender>();
}
else
{
    builder.Services.AddHttpClient<ResendAccountEmailSender>(client =>
    {
        client.BaseAddress = new Uri("https://api.resend.com/");
    });
    builder.Services.AddScoped<IAccountEmailSender>(serviceProvider =>
        serviceProvider.GetRequiredService<ResendAccountEmailSender>()
    );
}

builder.Services.AddAccountApplication();
builder.Services.AddAccountWebAdapters();
builder.Services.AddWebAdapters();
builder.Services.AddApplicationServices();
builder.Services.AddGameDataAdapters();

if (!builder.Environment.IsDevelopment())
{
    var keyRingPath = builder.Configuration["DataProtection:KeyRingPath"] ?? "/var/lib/spx/keys";
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
}

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapHub<NexusHub>("/hubs/game").RequireAuthorization();
app.MapAccountEndpoints();
app.MapGameEndpoints();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapDefaultEndpoints();

app.Run();
