using Microsoft.Extensions.Logging;
using Spx.Account.Application;

namespace Spx.Web.Adapters.Account;

public sealed partial class LoggingAccountEmailSender(
    ILogger<LoggingAccountEmailSender> logger,
    AccountLinkBuilder accountLinkBuilder
) : IAccountEmailSender
{
    public Task SendConfirmationEmailAsync(
        string email,
        string userId,
        string code,
        CancellationToken cancellationToken = default
    )
    {
        var confirmationLink = accountLinkBuilder.BuildConfirmationLink(userId, code);
        LogSendConfirmationEmail(logger, email, confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default
    )
    {
        var resetLink = accountLinkBuilder.BuildPasswordResetLink(email, code);
        LogSendPasswordResetEmail(logger, email, resetLink);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Development confirmation email for {Email}: {ConfirmationLink}"
    )]
    private static partial void LogSendConfirmationEmail(
        ILogger logger,
        string email,
        string confirmationLink
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Development password reset email for {Email}: {ResetLink}"
    )]
    private static partial void LogSendPasswordResetEmail(
        ILogger logger,
        string email,
        string resetLink
    );
}
