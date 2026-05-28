using Spx.Account.Application;

namespace Spx.Account.Application.Features.ResetPassword;

public interface IResetPasswordHandler
{
    Task<ResetPasswordOutcome> HandleAsync(
        string email,
        string code,
        string password,
        string confirmPassword
    );
}
