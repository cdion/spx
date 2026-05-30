using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spx.Data;
using Xunit;

namespace Spx.Web.Tests;

public sealed class IdentityCookieValidationTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("DataSource=:memory:");
    private WebApplication app = null!;

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        app = await CreateAppAsync(connection);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await app.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Authorized_request_redirects_to_login_when_cookie_user_no_longer_exists()
    {
        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            Email = "user@example.com",
            UserName = "user@example.com",
            EmailConfirmed = true,
        };

        var createResult = await userManager.CreateAsync(user, "Password1");
        Assert.True(createResult.Succeeded);

        var client = app.GetTestClient();
        var signInResponse = await client.PostAsync($"/test-login/{user.Id}", content: null);
        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);

        var authCookie = Assert.Single(signInResponse.Headers.GetValues("Set-Cookie")).Split(';')[
            0
        ];

        var secureRequest = new HttpRequestMessage(HttpMethod.Get, "/secure");
        secureRequest.Headers.Add("Cookie", authCookie);
        var secureResponse = await client.SendAsync(secureRequest);
        Assert.Equal(HttpStatusCode.OK, secureResponse.StatusCode);

        var deleteResponse = await client.PostAsync($"/test-delete/{user.Id}", content: null);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var staleCookieRequest = new HttpRequestMessage(HttpMethod.Get, "/secure");
        staleCookieRequest.Headers.Add("Cookie", authCookie);
        var staleCookieResponse = await client.SendAsync(staleCookieRequest);

        Assert.Equal(HttpStatusCode.Redirect, staleCookieResponse.StatusCode);
        Assert.Equal("/login", staleCookieResponse.Headers.Location?.AbsolutePath);
        Assert.Equal("?ReturnUrl=%2Fsecure", staleCookieResponse.Headers.Location?.Query);
        Assert.Contains(
            staleCookieResponse.Headers.GetValues("Set-Cookie"),
            static header =>
                header.Contains("Identity.Application=", StringComparison.Ordinal)
                && header.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase)
        );
    }

    private static async Task<WebApplication> CreateAppAsync(SqliteConnection connection)
    {
        var builder = WebApplication.CreateEmptyBuilder(
            new WebApplicationOptions { EnvironmentName = "Development" }
        );
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddLogging();
        builder.Services.AddDataProtection();
        builder.Services.AddHttpContextAccessor();
        builder
            .Services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/login";
        });
        builder.Services.Configure<SecurityStampValidatorOptions>(options =>
        {
            options.ValidationInterval = TimeSpan.Zero;
        });
        builder.Services.AddAuthorization();
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connection)
        );
        builder
            .Services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/login", () => Results.Ok());
        app.MapGet("/secure", [Authorize] () => Results.Ok("secure"));
        app.MapPost(
            "/test-login/{userId}",
            async Task<IResult> (
                string userId,
                UserManager<ApplicationUser> userManager,
                SignInManager<ApplicationUser> signInManager
            ) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user is null)
                {
                    return Results.NotFound();
                }

                await signInManager.SignInAsync(user, isPersistent: false);
                return Results.Ok();
            }
        );
        app.MapPost(
            "/test-delete/{userId}",
            async Task<IResult> (string userId, UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user is null)
                {
                    return Results.NotFound();
                }

                var result = await userManager.DeleteAsync(user);
                return result.Succeeded ? Results.Ok() : Results.BadRequest(result.Errors);
            }
        );

        await app.StartAsync();
        return app;
    }
}
