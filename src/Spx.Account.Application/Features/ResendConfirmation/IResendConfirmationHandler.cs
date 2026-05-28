using Spx.Account.Application;

namespace Spx.Account.Application.Features.ResendConfirmation;

public interface IResendConfirmationHandler
{
    Task<ResendConfirmationOutcome> HandleAsync(
        string email,
        CancellationToken cancellationToken = default
    );
}
