using Microsoft.Extensions.Logging;

namespace Spx.Web.Services.Email;

public sealed class LoggingAccountEmailSender(ILogger<LoggingAccountEmailSender> logger) : IAccountEmailSender
{
    public Task SendConfirmationLinkAsync(string email, string confirmationLink, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Development confirmation email for {Email}: {ConfirmationLink}", email, confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(string email, string resetLink, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Development password reset email for {Email}: {ResetLink}", email, resetLink);
        return Task.CompletedTask;
    }
}