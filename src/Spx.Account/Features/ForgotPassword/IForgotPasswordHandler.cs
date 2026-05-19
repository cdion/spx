using Spx.Account;

namespace Spx.Account.Features.ForgotPassword;

public interface IForgotPasswordHandler
{
    Task<ForgotPasswordOutcome> HandleAsync(
        string email,
        CancellationToken cancellationToken = default
    );
}
