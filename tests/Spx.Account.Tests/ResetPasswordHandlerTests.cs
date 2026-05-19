using Microsoft.Extensions.DependencyInjection;
using Spx.Account;
using Spx.Account.Features.ResetPassword;
using Xunit;

namespace Spx.Account.Tests;

public sealed class ResetPasswordHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_incomplete_link_when_required_values_are_missing()
    {
        using var services = AccountHandlerTestServices.Create(new FakeAccountIdentity());

        var handler = services.GetRequiredService<IResetPasswordHandler>();
        var outcome = await handler.HandleAsync(
            string.Empty,
            string.Empty,
            "Password1",
            "Password1"
        );

        Assert.Equal(ResetPasswordOutcomeStatus.IncompleteLink, outcome.Status);
    }

    [Fact]
    public async Task HandleAsync_returns_password_mismatch_when_passwords_differ()
    {
        using var services = AccountHandlerTestServices.Create(new FakeAccountIdentity());

        var handler = services.GetRequiredService<IResetPasswordHandler>();
        var outcome = await handler.HandleAsync(
            "user@example.com",
            "code",
            "Password1",
            "Password2"
        );

        Assert.Equal(ResetPasswordOutcomeStatus.PasswordMismatch, outcome.Status);
    }

    [Fact]
    public async Task HandleAsync_returns_completed_when_user_is_missing()
    {
        using var services = AccountHandlerTestServices.Create(new FakeAccountIdentity());

        var handler = services.GetRequiredService<IResetPasswordHandler>();
        var outcome = await handler.HandleAsync(
            "user@example.com",
            "code",
            "Password1",
            "Password1"
        );

        Assert.Equal(ResetPasswordOutcomeStatus.Completed, outcome.Status);
    }

    [Fact]
    public async Task HandleAsync_returns_failed_when_identity_reset_fails()
    {
        var identity = new FakeAccountIdentity
        {
            FindByEmailResult = new AccountUser("user-1", "user@example.com"),
            ResetPasswordResult = new AccountOperationFailed(["Reset failed."]),
        };
        using var services = AccountHandlerTestServices.Create(identity);

        var handler = services.GetRequiredService<IResetPasswordHandler>();
        var outcome = await handler.HandleAsync(
            "user@example.com",
            "code",
            "Password1",
            "Password1"
        );

        Assert.Equal(ResetPasswordOutcomeStatus.Failed, outcome.Status);
        Assert.Equal(["Reset failed."], outcome.Errors);
    }

    [Fact]
    public async Task HandleAsync_returns_completed_when_identity_reset_succeeds()
    {
        var identity = new FakeAccountIdentity
        {
            FindByEmailResult = new AccountUser("user-1", "user@example.com"),
            ResetPasswordResult = new AccountOperationSucceeded(),
        };
        using var services = AccountHandlerTestServices.Create(identity);

        var handler = services.GetRequiredService<IResetPasswordHandler>();
        var outcome = await handler.HandleAsync(
            "user@example.com",
            "code",
            "Password1",
            "Password1"
        );

        Assert.Equal(ResetPasswordOutcomeStatus.Completed, outcome.Status);
    }
}
