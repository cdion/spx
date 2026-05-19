using Microsoft.Extensions.Logging;
using Spx.Account;

namespace Spx.Account.Features.Register;

internal sealed partial class RegisterHandler(
    IAccountIdentity accountIdentity,
    IAccountEmailSender emailSender,
    ILogger<RegisterHandler> logger
) : IRegisterHandler
{
    private const string ConfirmationResendRequiredMessage =
        "Your account was created, but we could not send a confirmation email. Request a new one below.";

    public async Task<RegisterOutcome> HandleAsync(
        string email,
        string password,
        string confirmPassword,
        CancellationToken cancellationToken = default
    )
    {
        if (
            string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(confirmPassword)
        )
        {
            return new RegisterOutcome(RegisterOutcomeStatus.ValidationFailed);
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            return new RegisterOutcome(RegisterOutcomeStatus.PasswordMismatch, email);
        }

        var result = await accountIdentity.CreateUserAsync(email, password);
        if (result is not AccountCreateSucceeded created)
        {
            var failed = (AccountCreateFailed)result;
            return new RegisterOutcome(RegisterOutcomeStatus.Failed, email, failed.Errors);
        }

        var user = created.User;
        var code = await accountIdentity.GenerateEmailConfirmationTokenAsync(user);
        if (string.IsNullOrWhiteSpace(code))
        {
            return new RegisterOutcome(
                RegisterOutcomeStatus.ConfirmationResendRequired,
                email,
                [ConfirmationResendRequiredMessage]
            );
        }

        try
        {
            await emailSender.SendConfirmationEmailAsync(email, user.Id, code, cancellationToken);
        }
        catch (Exception exception)
        {
            LogSendConfirmationEmailFailed(logger, exception, email);
            return new RegisterOutcome(
                RegisterOutcomeStatus.ConfirmationResendRequired,
                email,
                [ConfirmationResendRequiredMessage]
            );
        }

        return new RegisterOutcome(RegisterOutcomeStatus.ConfirmationSent, email);
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to send registration confirmation email for {Email}."
    )]
    private static partial void LogSendConfirmationEmailFailed(
        ILogger logger,
        Exception exception,
        string email
    );
}
