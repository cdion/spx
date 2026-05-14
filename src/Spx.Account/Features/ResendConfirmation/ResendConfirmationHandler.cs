using Microsoft.Extensions.Logging;
using Spx.Account;

namespace Spx.Account.Features.ResendConfirmation;

internal sealed class ResendConfirmationHandler(
    IAccountIdentity accountIdentity,
    IAccountEmailSender emailSender,
    ILogger<ResendConfirmationHandler> logger) : IResendConfirmationHandler
{
    public async Task<ResendConfirmationOutcome> HandleAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return new ResendConfirmationOutcome(ResendConfirmationOutcomeStatus.ValidationFailed);
        }

        var user = await accountIdentity.FindByEmailAsync(email);
        if (user is not null && !await accountIdentity.IsEmailConfirmedAsync(user))
        {
            var code = await accountIdentity.GenerateEmailConfirmationTokenAsync(user);
            if (!string.IsNullOrWhiteSpace(code))
            {
                try
                {
                    await emailSender.SendConfirmationEmailAsync(email, user.Id, code, cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Failed to resend confirmation email for {Email}.", email);
                }
            }
        }

        return new ResendConfirmationOutcome(ResendConfirmationOutcomeStatus.Completed, email);
    }
}