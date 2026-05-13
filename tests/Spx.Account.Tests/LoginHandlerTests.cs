using Microsoft.Extensions.DependencyInjection;
using Spx.Account;
using Spx.Account.Features.Login;
using Xunit;

namespace Spx.Account.Tests;

public sealed class LoginHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_validation_failed_when_credentials_are_missing()
    {
        using var services = AccountHandlerTestServices.Create(new FakeAccountIdentity());

        var handler = services.GetRequiredService<ILoginHandler>();
        var outcome = await handler.HandleAsync(string.Empty, string.Empty, "/games");

        Assert.Equal(LoginOutcomeStatus.ValidationFailed, outcome.Status);
        Assert.Equal("/games", outcome.ReturnUrl);
    }

    [Fact]
    public async Task HandleAsync_returns_invalid_credentials_when_user_is_missing()
    {
        using var services = AccountHandlerTestServices.Create(new FakeAccountIdentity());

        var handler = services.GetRequiredService<ILoginHandler>();
        var outcome = await handler.HandleAsync("user@example.com", "Password1", "/games");

        Assert.Equal(LoginOutcomeStatus.InvalidCredentials, outcome.Status);
        Assert.Equal("/games", outcome.ReturnUrl);
    }

    [Fact]
    public async Task HandleAsync_returns_email_confirmation_required_when_identity_requires_confirmation()
    {
        var identity = new FakeAccountIdentity
        {
            FindByEmailResult = new AccountUser("user-1", "user@example.com"),
            PasswordSignInResult = new AccountPasswordSignInOutcome(AccountPasswordSignInStatus.EmailConfirmationRequired)
        };
        using var services = AccountHandlerTestServices.Create(identity);

        var handler = services.GetRequiredService<ILoginHandler>();
        var outcome = await handler.HandleAsync("user@example.com", "Password1", "/games");

        Assert.Equal(LoginOutcomeStatus.EmailConfirmationRequired, outcome.Status);
        Assert.Equal("/games", outcome.ReturnUrl);
        Assert.Equal("user@example.com", outcome.Email);
    }

    [Fact]
    public async Task HandleAsync_returns_locked_out_when_identity_reports_lockout()
    {
        var identity = new FakeAccountIdentity
        {
            FindByEmailResult = new AccountUser("user-1", "user@example.com"),
            PasswordSignInResult = new AccountPasswordSignInOutcome(AccountPasswordSignInStatus.LockedOut)
        };
        using var services = AccountHandlerTestServices.Create(identity);

        var handler = services.GetRequiredService<ILoginHandler>();
        var outcome = await handler.HandleAsync("user@example.com", "Password1", "/games");

        Assert.Equal(LoginOutcomeStatus.LockedOut, outcome.Status);
        Assert.Equal("/games", outcome.ReturnUrl);
    }

    [Fact]
    public async Task HandleAsync_rejects_unsafe_return_urls()
    {
        var identity = new FakeAccountIdentity
        {
            FindByEmailResult = new AccountUser("user-1", "user@example.com"),
            PasswordSignInResult = new AccountPasswordSignInOutcome(AccountPasswordSignInStatus.Succeeded)
        };
        using var services = AccountHandlerTestServices.Create(identity);

        var handler = services.GetRequiredService<ILoginHandler>();
        var outcome = await handler.HandleAsync("user@example.com", "Password1", "//evil.test");

        Assert.Equal(LoginOutcomeStatus.Succeeded, outcome.Status);
        Assert.Equal("/", outcome.ReturnUrl);
    }
}