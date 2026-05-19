using Spx.Account;

namespace Spx.Account.Features.ResetPassword;

internal sealed class ResetPasswordHandler(IAccountIdentity accountIdentity) : IResetPasswordHandler
{
    public async Task<ResetPasswordOutcome> HandleAsync(
        string email,
        string code,
        string password,
        string confirmPassword
    )
    {
        if (
            string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(code)
            || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(confirmPassword)
        )
        {
            return new ResetPasswordOutcome(ResetPasswordOutcomeStatus.IncompleteLink, email, code);
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            return new ResetPasswordOutcome(
                ResetPasswordOutcomeStatus.PasswordMismatch,
                email,
                code
            );
        }

        var user = await accountIdentity.FindByEmailAsync(email);
        if (user is null)
        {
            return new ResetPasswordOutcome(ResetPasswordOutcomeStatus.Completed, email, code);
        }

        var result = await accountIdentity.ResetPasswordAsync(user, code, password);
        return result is AccountOperationSucceeded
            ? new ResetPasswordOutcome(ResetPasswordOutcomeStatus.Completed, email, code)
            : new ResetPasswordOutcome(
                ResetPasswordOutcomeStatus.Failed,
                email,
                code,
                ((AccountOperationFailed)result).Errors
            );
    }
}
