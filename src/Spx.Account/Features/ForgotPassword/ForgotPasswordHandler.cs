using Microsoft.Extensions.Logging;
using Spx.Account;

namespace Spx.Account.Features.ForgotPassword;

internal sealed partial class ForgotPasswordHandler(
    IAccountIdentity accountIdentity,
    IAccountEmailSender emailSender,
    ILogger<ForgotPasswordHandler> logger
) : IForgotPasswordHandler
{
    public async Task<ForgotPasswordOutcome> HandleAsync(
        string email,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return new ForgotPasswordOutcome(ForgotPasswordOutcomeStatus.ValidationFailed);
        }

        var user = await accountIdentity.FindByEmailAsync(email);
        if (user is not null && await accountIdentity.IsEmailConfirmedAsync(user))
        {
            var code = await accountIdentity.GeneratePasswordResetTokenAsync(user);
            if (!string.IsNullOrWhiteSpace(code))
            {
                try
                {
                    await emailSender.SendPasswordResetEmailAsync(email, code, cancellationToken);
                }
                catch (Exception exception)
                {
                    LogSendPasswordResetEmailFailed(logger, exception, email);
                }
            }
        }

        return new ForgotPasswordOutcome(ForgotPasswordOutcomeStatus.Completed);
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to send password reset email for {Email}."
    )]
    private static partial void LogSendPasswordResetEmailFailed(
        ILogger logger,
        Exception exception,
        string email
    );
}
