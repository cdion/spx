using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spx.Account;
using Spx.Data;
using Spx.Web.Adapters.Account;
using Xunit;

namespace Spx.Web.Tests;

public sealed class IdentityAccountIdentityAdapterTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("DataSource=:memory:");
    private ServiceProvider services = null!;

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddDataProtection();
        serviceCollection.AddHttpContextAccessor();
        serviceCollection
            .AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();
        serviceCollection.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connection)
        );
        serviceCollection
            .AddIdentityCore<ApplicationUser>(options =>
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

        serviceCollection.AddAccountWebAdapters();

        services = serviceCollection.BuildServiceProvider();

        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        httpContextAccessor.HttpContext = new DefaultHttpContext();
    }

    public async Task DisposeAsync()
    {
        await services.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Password_sign_in_returns_failed_when_user_disappears()
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var accountIdentity = scope.ServiceProvider.GetRequiredService<IAccountIdentity>();

        var user = new ApplicationUser
        {
            Email = "user@example.com",
            UserName = "user@example.com",
            EmailConfirmed = true,
        };
        var createResult = await userManager.CreateAsync(user, "Password1");
        Assert.True(createResult.Succeeded);

        await userManager.DeleteAsync(user);

        var result = await accountIdentity.PasswordSignInAsync(
            new AccountUser(user.Id, user.Email!),
            "Password1"
        );

        Assert.Equal(AccountPasswordSignInStatus.Failed, result.Status);
    }

    [Fact]
    public async Task Reset_password_returns_failure_when_user_disappears()
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var accountIdentity = scope.ServiceProvider.GetRequiredService<IAccountIdentity>();

        var user = new ApplicationUser
        {
            Email = "user@example.com",
            UserName = "user@example.com",
            EmailConfirmed = true,
        };
        var createResult = await userManager.CreateAsync(user, "Password1");
        Assert.True(createResult.Succeeded);

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        await userManager.DeleteAsync(user);

        var result = await Record.ExceptionAsync(() =>
            accountIdentity.ResetPasswordAsync(
                new AccountUser(user.Id, user.Email!),
                token,
                "NewPassword1"
            )
        );

        Assert.Null(result);
        var operation = await accountIdentity.ResetPasswordAsync(
            new AccountUser(user.Id, user.Email!),
            token,
            "NewPassword1"
        );
        Assert.IsType<AccountOperationFailed>(operation);
    }

    [Fact]
    public async Task Confirm_email_round_trip_succeeds_for_existing_user()
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var accountIdentity = scope.ServiceProvider.GetRequiredService<IAccountIdentity>();

        var createResult = await accountIdentity.CreateUserAsync("user@example.com", "Password1");
        var createSucceeded = Assert.IsType<AccountCreateSucceeded>(createResult);
        var user = createSucceeded.User;

        var token = await accountIdentity.GenerateEmailConfirmationTokenAsync(user);
        var confirmResult = await accountIdentity.ConfirmEmailAsync(user, token);

        Assert.IsType<AccountOperationSucceeded>(confirmResult);
        var storedUser = await userManager.FindByIdAsync(user.Id);
        Assert.NotNull(storedUser);
        Assert.True(storedUser!.EmailConfirmed);
    }
}
