using Microsoft.Extensions.DependencyInjection;
using Spx.Account;
using Spx.Account.Features.ForgotPassword;
using Xunit;

namespace Spx.Account.Tests;

public sealed class ForgotPasswordHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_validation_failed_when_email_is_missing()
    {
        using var services = AccountHandlerTestServices.Create(new FakeAccountIdentity());

        var handler = services.GetRequiredService<IForgotPasswordHandler>();
        var outcome = await handler.HandleAsync(string.Empty);

        Assert.Equal(ForgotPasswordOutcomeStatus.ValidationFailed, outcome.Status);
    }

    [Fact]
    public async Task HandleAsync_sends_reset_email_for_confirmed_user_when_token_is_available()
    {
        var identity = new FakeAccountIdentity
        {
            FindByEmailResult = new AccountUser("user-1", "user@example.com"),
            IsEmailConfirmedResult = true,
            PasswordResetToken = "reset-token",
        };
        var emailSender = new FakeAccountEmailSender();
        using var services = AccountHandlerTestServices.Create(identity, emailSender);

        var handler = services.GetRequiredService<IForgotPasswordHandler>();
        var outcome = await handler.HandleAsync("user@example.com");

        Assert.Equal(ForgotPasswordOutcomeStatus.Completed, outcome.Status);
        Assert.True(emailSender.PasswordResetEmailSent);
        Assert.Equal("user@example.com", emailSender.LastPasswordResetEmail);
        Assert.Equal("reset-token", emailSender.LastPasswordResetCode);
    }

    [Fact]
    public async Task HandleAsync_does_not_send_for_unconfirmed_user()
    {
        var identity = new FakeAccountIdentity
        {
            FindByEmailResult = new AccountUser("user-1", "user@example.com"),
            IsEmailConfirmedResult = false,
            PasswordResetToken = "reset-token",
        };
        var emailSender = new FakeAccountEmailSender();
        using var services = AccountHandlerTestServices.Create(identity, emailSender);

        var handler = services.GetRequiredService<IForgotPasswordHandler>();
        var outcome = await handler.HandleAsync("user@example.com");

        Assert.Equal(ForgotPasswordOutcomeStatus.Completed, outcome.Status);
        Assert.False(emailSender.PasswordResetEmailSent);
    }

    [Fact]
    public async Task HandleAsync_does_not_send_when_reset_token_is_missing()
    {
        var identity = new FakeAccountIdentity
        {
            FindByEmailResult = new AccountUser("user-1", "user@example.com"),
            IsEmailConfirmedResult = true,
            PasswordResetToken = string.Empty,
        };
        var emailSender = new FakeAccountEmailSender();
        using var services = AccountHandlerTestServices.Create(identity, emailSender);

        var handler = services.GetRequiredService<IForgotPasswordHandler>();
        var outcome = await handler.HandleAsync("user@example.com");

        Assert.Equal(ForgotPasswordOutcomeStatus.Completed, outcome.Status);
        Assert.False(emailSender.PasswordResetEmailSent);
    }

    [Fact]
    public async Task HandleAsync_completes_when_email_sender_fails()
    {
        var identity = new FakeAccountIdentity
        {
            FindByEmailResult = new AccountUser("user-1", "user@example.com"),
            IsEmailConfirmedResult = true,
            PasswordResetToken = "reset-token",
        };
        var emailSender = new FakeAccountEmailSender
        {
            SendPasswordResetException = new InvalidOperationException("mail failed"),
        };
        using var services = AccountHandlerTestServices.Create(identity, emailSender);

        var handler = services.GetRequiredService<IForgotPasswordHandler>();
        var outcome = await handler.HandleAsync("user@example.com");

        Assert.Equal(ForgotPasswordOutcomeStatus.Completed, outcome.Status);
        Assert.False(emailSender.PasswordResetEmailSent);
    }

    [Fact]
    public async Task HandleAsync_completes_without_sending_for_unknown_user()
    {
        var identity = new FakeAccountIdentity();
        var emailSender = new FakeAccountEmailSender();
        using var services = AccountHandlerTestServices.Create(identity, emailSender);

        var handler = services.GetRequiredService<IForgotPasswordHandler>();
        var outcome = await handler.HandleAsync("user@example.com");

        Assert.Equal(ForgotPasswordOutcomeStatus.Completed, outcome.Status);
        Assert.False(emailSender.PasswordResetEmailSent);
    }
}
