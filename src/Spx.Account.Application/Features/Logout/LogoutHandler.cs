using Spx.Account.Application;

namespace Spx.Account.Application.Features.Logout;

internal sealed class LogoutHandler(IAccountIdentity accountIdentity) : ILogoutHandler
{
    public async Task<LogoutOutcome> HandleAsync()
    {
        await accountIdentity.SignOutAsync();
        return new LogoutOutcome();
    }
}
