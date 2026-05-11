using Microsoft.Extensions.DependencyInjection;
using Spx.Account;
using Spx.Account.Features.Register;
using Xunit;

namespace Spx.Account.Tests;

public sealed class RegisterHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_validation_failed_when_required_fields_are_missing()
    {
        using var services = AccountHandlerTestServices.Create(new FakeAccountIdentity());

        var handler = services.GetRequiredService<IRegisterHandler>();
        var outcome = await handler.HandleAsync(string.Empty, "Password1", "Password1");

        Assert.Equal(RegisterOutcomeStatus.ValidationFailed, outcome.Status);
    }

    [Fact]
    public async Task HandleAsync_returns_password_mismatch_when_passwords_differ()
    {
        using var services = AccountHandlerTestServices.Create(new FakeAccountIdentity());

        var handler = services.GetRequiredService<IRegisterHandler>();
        var outcome = await handler.HandleAsync("user@example.com", "Password1", "Password2");

        Assert.Equal(RegisterOutcomeStatus.PasswordMismatch, outcome.Status);
        Assert.Equal("user@example.com", outcome.Email);
    }

    [Fact]
    public async Task HandleAsync_returns_failed_when_user_creation_fails()
    {
        var identity = new FakeAccountIdentity
        {
            CreateUserResult = new AccountCreateResult(null, false, ["Password is too weak."])
        };
        var emailSender = new FakeAccountEmailSender();
        using var services = AccountHandlerTestServices.Create(identity, emailSender);

        var handler = services.GetRequiredService<IRegisterHandler>();
        var outcome = await handler.HandleAsync("user@example.com", "Password1", "Password1");

        Assert.Equal(RegisterOutcomeStatus.Failed, outcome.Status);
        Assert.Equal(["Password is too weak."], outcome.Errors);
        Assert.False(emailSender.ConfirmationEmailSent);
    }

    [Fact]
    public async Task HandleAsync_returns_failed_when_confirmation_token_is_missing()
    {
        var identity = new FakeAccountIdentity
        {
            CreateUserResult = new AccountCreateResult(new AccountUser("user-1", "user@example.com"), true, []),
            EmailConfirmationToken = string.Empty
        };
        var emailSender = new FakeAccountEmailSender();
        using var services = AccountHandlerTestServices.Create(identity, emailSender);

        var handler = services.GetRequiredService<IRegisterHandler>();
        var outcome = await handler.HandleAsync("user@example.com", "Password1", "Password1");

        Assert.Equal(RegisterOutcomeStatus.Failed, outcome.Status);
        Assert.Equal("user@example.com", outcome.Email);
        Assert.Equal(["We could not generate a confirmation email. Please try again."], outcome.Errors);
        Assert.False(emailSender.ConfirmationEmailSent);
    }

    [Fact]
    public async Task HandleAsync_sends_confirmation_email_when_registration_succeeds()
    {
        var identity = new FakeAccountIdentity
        {
            CreateUserResult = new AccountCreateResult(new AccountUser("user-1", "user@example.com"), true, []),
            EmailConfirmationToken = "confirm-token"
        };
        var emailSender = new FakeAccountEmailSender();
        using var services = AccountHandlerTestServices.Create(identity, emailSender);

        var handler = services.GetRequiredService<IRegisterHandler>();
        var outcome = await handler.HandleAsync("user@example.com", "Password1", "Password1");

        Assert.Equal(RegisterOutcomeStatus.ConfirmationSent, outcome.Status);
        Assert.True(emailSender.ConfirmationEmailSent);
        Assert.Equal("user@example.com", emailSender.LastConfirmationEmail);
        Assert.Equal("user-1", emailSender.LastConfirmationUserId);
        Assert.Equal("confirm-token", emailSender.LastConfirmationCode);
    }

}