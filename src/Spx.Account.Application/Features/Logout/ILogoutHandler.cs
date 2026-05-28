namespace Spx.Account.Application.Features.Logout;

public interface ILogoutHandler
{
    Task<LogoutOutcome> HandleAsync();
}
