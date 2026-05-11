using Spx.Account;

namespace Spx.Account.Features.ForgotPassword;

internal sealed class ForgotPasswordHandler(
    IAccountIdentity accountIdentity,
    IAccountEmailSender emailSender) : IForgotPasswordHandler
{
    public async Task<ForgotPasswordOutcome> HandleAsync(string email, CancellationToken cancellationToken = default)
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
                await emailSender.SendPasswordResetEmailAsync(email, code, cancellationToken);
            }
        }

        return new ForgotPasswordOutcome(ForgotPasswordOutcomeStatus.Completed);
    }
}