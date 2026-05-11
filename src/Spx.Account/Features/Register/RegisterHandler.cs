using Spx.Account;

namespace Spx.Account.Features.Register;

internal sealed class RegisterHandler(
    IAccountIdentity accountIdentity,
    IAccountEmailSender emailSender) : IRegisterHandler
{
    public async Task<RegisterOutcome> HandleAsync(string email, string password, string confirmPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
        {
            return new RegisterOutcome(RegisterOutcomeStatus.ValidationFailed);
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            return new RegisterOutcome(RegisterOutcomeStatus.PasswordMismatch, email);
        }

        var result = await accountIdentity.CreateUserAsync(email, password);
        if (!result.Succeeded)
        {
            return new RegisterOutcome(RegisterOutcomeStatus.Failed, email, result.Errors);
        }

        var user = result.User!;
        var code = await accountIdentity.GenerateEmailConfirmationTokenAsync(user);
        if (string.IsNullOrWhiteSpace(code))
        {
            return new RegisterOutcome(RegisterOutcomeStatus.Failed, email, ["We could not generate a confirmation email. Please try again."]);
        }

        await emailSender.SendConfirmationEmailAsync(email, user.Id, code, cancellationToken);

        return new RegisterOutcome(RegisterOutcomeStatus.ConfirmationSent, email);
    }
}