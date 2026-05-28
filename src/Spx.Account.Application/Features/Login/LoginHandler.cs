using Spx.Account.Application;

namespace Spx.Account.Application.Features.Login;

internal sealed class LoginHandler(IAccountIdentity accountIdentity) : ILoginHandler
{
    public async Task<LoginOutcome> HandleAsync(string email, string password, string? returnUrl)
    {
        var resolvedReturnUrl = ResolveReturnUrl(returnUrl);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return new LoginOutcome(LoginOutcomeStatus.ValidationFailed, resolvedReturnUrl);
        }

        var user = await accountIdentity.FindByEmailAsync(email);
        if (user is null)
        {
            return new LoginOutcome(LoginOutcomeStatus.InvalidCredentials, resolvedReturnUrl);
        }

        var result = await accountIdentity.PasswordSignInAsync(user, password);
        if (result.Status == AccountPasswordSignInStatus.Succeeded)
        {
            return new LoginOutcome(LoginOutcomeStatus.Succeeded, resolvedReturnUrl);
        }

        if (result.Status == AccountPasswordSignInStatus.EmailConfirmationRequired)
        {
            return new LoginOutcome(
                LoginOutcomeStatus.EmailConfirmationRequired,
                resolvedReturnUrl,
                email
            );
        }

        if (result.Status == AccountPasswordSignInStatus.LockedOut)
        {
            return new LoginOutcome(LoginOutcomeStatus.LockedOut, resolvedReturnUrl);
        }

        return new LoginOutcome(LoginOutcomeStatus.InvalidCredentials, resolvedReturnUrl);
    }

    private static string ResolveReturnUrl(string? returnUrl)
    {
        if (
            !string.IsNullOrWhiteSpace(returnUrl)
            && returnUrl.StartsWith('/')
            && !returnUrl.StartsWith("//", StringComparison.Ordinal)
        )
        {
            return returnUrl;
        }

        return "/";
    }
}
