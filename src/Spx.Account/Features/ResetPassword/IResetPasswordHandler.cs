using Spx.Account;

namespace Spx.Account.Features.ResetPassword;

public interface IResetPasswordHandler
{
    Task<ResetPasswordOutcome> HandleAsync(
        string email,
        string code,
        string password,
        string confirmPassword
    );
}
