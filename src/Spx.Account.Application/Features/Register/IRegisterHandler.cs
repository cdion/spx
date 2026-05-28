using Spx.Account.Application;

namespace Spx.Account.Application.Features.Register;

public interface IRegisterHandler
{
    Task<RegisterOutcome> HandleAsync(
        string email,
        string password,
        string confirmPassword,
        CancellationToken cancellationToken = default
    );
}
