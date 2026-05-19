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
            CreateUserResult = new AccountCreateFailed(["Password is too weak."]),
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
    public async Task HandleAsync_returns_confirmation_resend_required_when_confirmation_token_is_missing()
    {
        var identity = new FakeAccountIdentity
        {
            CreateUserResult = new AccountCreateSucceeded(
                new AccountUser("user-1", "user@example.com")
            ),
            EmailConfirmationToken = string.Empty,
        };
        var emailSender = new FakeAccountEmailSender();
        using var services = AccountHandlerTestServices.Create(identity, emailSender);

        var handler = services.GetRequiredService<IRegisterHandler>();
        var outcome = await handler.HandleAsync("user@example.com", "Password1", "Password1");

        Assert.Equal(RegisterOutcomeStatus.ConfirmationResendRequired, outcome.Status);
        Assert.Equal("user@example.com", outcome.Email);
        Assert.Equal(
            [
                "Your account was created, but we could not send a confirmation email. Request a new one below.",
            ],
            outcome.Errors
        );
        Assert.False(emailSender.ConfirmationEmailSent);
    }

    [Fact]
    public async Task HandleAsync_returns_confirmation_resend_required_when_confirmation_email_send_fails()
    {
        var identity = new FakeAccountIdentity
        {
            CreateUserResult = new AccountCreateSucceeded(
                new AccountUser("user-1", "user@example.com")
            ),
            EmailConfirmationToken = "confirm-token",
        };
        var emailSender = new FakeAccountEmailSender
        {
            SendConfirmationException = new InvalidOperationException("mail transport unavailable"),
        };
        using var services = AccountHandlerTestServices.Create(identity, emailSender);

        var handler = services.GetRequiredService<IRegisterHandler>();
        var outcome = await handler.HandleAsync("user@example.com", "Password1", "Password1");

        Assert.Equal(RegisterOutcomeStatus.ConfirmationResendRequired, outcome.Status);
        Assert.Equal("user@example.com", outcome.Email);
        Assert.Equal(
            [
                "Your account was created, but we could not send a confirmation email. Request a new one below.",
            ],
            outcome.Errors
        );
        Assert.False(emailSender.ConfirmationEmailSent);
    }

    [Fact]
    public async Task HandleAsync_sends_confirmation_email_when_registration_succeeds()
    {
        var identity = new FakeAccountIdentity
        {
            CreateUserResult = new AccountCreateSucceeded(
                new AccountUser("user-1", "user@example.com")
            ),
            EmailConfirmationToken = "confirm-token",
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
