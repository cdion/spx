using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Spx.Account;

namespace Spx.Web.Adapters.Account;

public sealed class LoggingAccountEmailSender(
    ILogger<LoggingAccountEmailSender> logger,
    IHttpContextAccessor httpContextAccessor) : IAccountEmailSender
{
    public Task SendConfirmationEmailAsync(string email, string userId, string code, CancellationToken cancellationToken = default)
    {
        var confirmationLink = BuildAbsoluteUri("/account/confirm-email", ("userId", userId), ("code", code));
        logger.LogInformation("Development confirmation email for {Email}: {ConfirmationLink}", email, confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string email, string code, CancellationToken cancellationToken = default)
    {
        var resetLink = BuildAbsoluteUri("/reset-password", ("email", email), ("code", code));
        logger.LogInformation("Development password reset email for {Email}: {ResetLink}", email, resetLink);
        return Task.CompletedTask;
    }

    private string BuildAbsoluteUri(string path, params (string Key, string? Value)[] values)
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No current HTTP request is available for building account links.");
        var query = new QueryBuilder();

        foreach (var (key, value) in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                query.Add(key, value);
            }
        }

        return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{path}{query}";
    }
}