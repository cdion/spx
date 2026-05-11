using Spx.Account;

namespace Spx.Account.Features.ResendConfirmation;

public interface IResendConfirmationHandler
{
    Task<ResendConfirmationOutcome> HandleAsync(string email, CancellationToken cancellationToken = default);
}