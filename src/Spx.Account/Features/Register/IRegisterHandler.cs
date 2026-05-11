using Spx.Account;

namespace Spx.Account.Features.Register;

public interface IRegisterHandler
{
    Task<RegisterOutcome> HandleAsync(string email, string password, string confirmPassword, CancellationToken cancellationToken = default);
}