using Spx.Account;

namespace Spx.Account.Features.Logout;

internal sealed class LogoutHandler(IAccountIdentity accountIdentity) : ILogoutHandler
{
    public async Task<LogoutOutcome> HandleAsync()
    {
        await accountIdentity.SignOutAsync();
        return new LogoutOutcome();
    }
}
