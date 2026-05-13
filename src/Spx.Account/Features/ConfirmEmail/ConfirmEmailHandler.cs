using Spx.Account;

namespace Spx.Account.Features.ConfirmEmail;

internal sealed class ConfirmEmailHandler(IAccountIdentity accountIdentity) : IConfirmEmailHandler
{
    public async Task<ConfirmEmailOutcome> HandleAsync(string userId, string code)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
        {
            return new ConfirmEmailOutcome(ConfirmEmailOutcomeStatus.InvalidLink);
        }

        var user = await accountIdentity.FindByIdAsync(userId);
        if (user is null)
        {
            return new ConfirmEmailOutcome(ConfirmEmailOutcomeStatus.InvalidLink);
        }

        var result = await accountIdentity.ConfirmEmailAsync(user, code);
        return result is AccountOperationSucceeded
            ? new ConfirmEmailOutcome(ConfirmEmailOutcomeStatus.Succeeded, user.Email)
            : new ConfirmEmailOutcome(ConfirmEmailOutcomeStatus.Failed, user.Email);
    }
}