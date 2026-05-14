using Microsoft.Extensions.Logging;
using Spx.Account;

namespace Spx.Web.Adapters.Account;

public sealed class LoggingAccountEmailSender(
    ILogger<LoggingAccountEmailSender> logger,
    AccountLinkBuilder accountLinkBuilder) : IAccountEmailSender
{
    public Task SendConfirmationEmailAsync(string email, string userId, string code, CancellationToken cancellationToken = default)
    {
        var confirmationLink = accountLinkBuilder.BuildConfirmationLink(userId, code);
        logger.LogInformation("Development confirmation email for {Email}: {ConfirmationLink}", email, confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string email, string code, CancellationToken cancellationToken = default)
    {
        var resetLink = accountLinkBuilder.BuildPasswordResetLink(email, code);
        logger.LogInformation("Development password reset email for {Email}: {ResetLink}", email, resetLink);
        return Task.CompletedTask;
    }
}