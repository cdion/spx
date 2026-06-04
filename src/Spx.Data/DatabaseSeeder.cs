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
public sealed partial class DatabaseSeeder(
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
            LogSkippingSeed(logger);
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
            LogSeededUser(logger, user.Email);
        }
        else
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            LogSeedFailed(logger, errors);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "Users already exist, skipping seed")]
    private static partial void LogSkippingSeed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded user: {Email}")]
    private static partial void LogSeededUser(ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to seed user: {Errors}")]
    private static partial void LogSeedFailed(ILogger logger, string errors);
}
