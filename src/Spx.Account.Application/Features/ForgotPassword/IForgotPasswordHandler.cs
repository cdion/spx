using Spx.Account.Application;

namespace Spx.Account.Application.Features.ForgotPassword;

public interface IForgotPasswordHandler
{
    Task<ForgotPasswordOutcome> HandleAsync(
        string email,
        CancellationToken cancellationToken = default
    );
}
