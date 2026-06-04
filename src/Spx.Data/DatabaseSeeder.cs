using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Spx.Data;

/// <summary>
/// Seeds reference / dev data on first startup.
/// Does NOT run EF migrations — that is handled by the deployment's migrations bundle
/// (see deploy/Containerfile). This seeder assumes the schema is already current.
/// </summary>
public sealed class DatabaseSeeder(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseSeeder> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        if (await userManager.Users.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Users already exist, skipping seed");
            return;
        }

        var user = new ApplicationUser
        {
            UserName = "chris@mostlyhuman.ca",
            Email = "chris@mostlyhuman.ca",
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, "Password12345");
        if (result.Succeeded)
        {
            logger.LogInformation("Seeded user: {Email}", user.Email);
        }
        else
        {
            logger.LogWarning(
                "Failed to seed user: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description))
            );
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
