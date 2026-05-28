namespace Spx.Account.Application.Features.Login;

public interface ILoginHandler
{
    Task<LoginOutcome> HandleAsync(string email, string password, string? returnUrl);
}
