using Microsoft.Extensions.DependencyInjection;
using Spx.Account;
using Spx.Account.Features.ConfirmEmail;
using Xunit;

namespace Spx.Account.Tests;

public sealed class ConfirmEmailHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_invalid_link_when_required_values_are_missing()
    {
        using var services = AccountHandlerTestServices.Create(new FakeAccountIdentity());

        var handler = services.GetRequiredService<IConfirmEmailHandler>();
        var outcome = await handler.HandleAsync(string.Empty, string.Empty);

        Assert.Equal(ConfirmEmailOutcomeStatus.InvalidLink, outcome.Status);
    }

    [Fact]
    public async Task HandleAsync_returns_invalid_link_when_user_cannot_be_found()
    {
        using var services = AccountHandlerTestServices.Create(new FakeAccountIdentity());

        var handler = services.GetRequiredService<IConfirmEmailHandler>();
        var outcome = await handler.HandleAsync("user-1", "code");

        Assert.Equal(ConfirmEmailOutcomeStatus.InvalidLink, outcome.Status);
    }

    [Fact]
    public async Task HandleAsync_returns_succeeded_when_confirmation_succeeds()
    {
        var identity = new FakeAccountIdentity
        {
            FindByIdResult = new AccountUser("user-1", "user@example.com"),
            ConfirmEmailResult = new AccountOperationResult(true, [])
        };
        using var services = AccountHandlerTestServices.Create(identity);

        var handler = services.GetRequiredService<IConfirmEmailHandler>();
        var outcome = await handler.HandleAsync("user-1", "code");

        Assert.Equal(ConfirmEmailOutcomeStatus.Succeeded, outcome.Status);
        Assert.Equal("user@example.com", outcome.Email);
    }

    [Fact]
    public async Task HandleAsync_returns_failed_when_confirmation_fails()
    {
        var identity = new FakeAccountIdentity
        {
            FindByIdResult = new AccountUser("user-1", "user@example.com"),
            ConfirmEmailResult = new AccountOperationResult(false, ["Invalid token."])
        };
        using var services = AccountHandlerTestServices.Create(identity);

        var handler = services.GetRequiredService<IConfirmEmailHandler>();
        var outcome = await handler.HandleAsync("user-1", "code");

        Assert.Equal(ConfirmEmailOutcomeStatus.Failed, outcome.Status);
        Assert.Equal("user@example.com", outcome.Email);
    }
}