using Spx.Account;

namespace Spx.Account.Features.ResendConfirmation;

internal sealed class ResendConfirmationHandler(
    IAccountIdentity accountIdentity,
    IAccountEmailSender emailSender) : IResendConfirmationHandler
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
                await emailSender.SendConfirmationEmailAsync(email, user.Id, code, cancellationToken);
            }
        }

        return new ResendConfirmationOutcome(ResendConfirmationOutcomeStatus.Completed, email);
    }
}