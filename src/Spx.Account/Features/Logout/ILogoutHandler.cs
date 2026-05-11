namespace Spx.Account.Features.Logout;

public interface ILogoutHandler
{
    Task<LogoutOutcome> HandleAsync();
}