using Microsoft.Extensions.DependencyInjection;
using Spx.Account.Application;
using Spx.Account.Application.Features.ResendConfirmation;
using Xunit;

namespace Spx.Account.Application.Tests;

public sealed class ResendConfirmationHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_validation_failed_when_email_is_missing()
    {
        using var services = AccountHandlerTestServices.Create(new FakeAccountIdentity());

        var handler = services.GetRequiredService<IResendConfirmationHandler>();
        var outcome = await handler.HandleAsync(string.Empty);

        Assert.Equal(ResendConfirmationOutcomeStatus.ValidationFailed, outcome.Status);
    }

    [Fact]
    public async Task HandleAsync_sends_confirmation_for_unconfirmed_user_when_token_is_available()
    {
        var identity = new FakeAccountIdentity
        {
            FindByEmailResult = new AccountUser("user-1", "user@example.com"),
            IsEmailConfirmedResult = false,
            EmailConfirmationToken = "confirm-token",
        };
        var emailSender = new FakeAccountEmailSender();
        using var services = AccountHandlerTestServices.Create(identity, emailSender);

        var handler = services.GetRequiredService<IResendConfirmationHandler>();
        var outcome = await handler.HandleAsync("user@example.com");

        Assert.Equal(ResendConfirmationOutcomeStatus.Completed, outcome.Status);
        Assert.True(emailSender.ConfirmationEmailSent);
        Assert.Equal("user@example.com", emailSender.LastConfirmationEmail);
        Assert.Equal("user-1", emailSender.LastConfirmationUserId);
        Assert.Equal("confirm-token", emailSender.LastConfirmationCode);
    }

    [Fact]
    public async Task HandleAsync_does_not_send_when_user_is_already_confirmed()
    {
        var identity = new FakeAccountIdentity
        {
            FindByEmailResult = new AccountUser("user-1", "user@example.com"),
            IsEmailConfirmedResult = true,
            EmailConfirmationToken = "confirm-token",
        };
        var emailSender = new FakeAccountEmailSender();
        using var services = AccountHandlerTestServices.Create(identity, emailSender);

        var handler = services.GetRequiredService<IResendConfirmationHandler>();
        var outcome = await handler.HandleAsync("user@example.com");

        Assert.Equal(ResendConfirmationOutcomeStatus.Completed, outcome.Status);
        Assert.False(emailSender.ConfirmationEmailSent);
    }

    [Fact]
    public async Task HandleAsync_completes_without_sending_when_user_is_missing()
    {
        var emailSender = new FakeAccountEmailSender();
        using var services = AccountHandlerTestServices.Create(
            new FakeAccountIdentity(),
            emailSender
        );

        var handler = services.GetRequiredService<IResendConfirmationHandler>();
        var outcome = await handler.HandleAsync("user@example.com");

        Assert.Equal(ResendConfirmationOutcomeStatus.Completed, outcome.Status);
        Assert.False(emailSender.ConfirmationEmailSent);
    }

    [Fact]
    public async Task HandleAsync_completes_without_sending_when_confirmation_token_is_missing()
    {
        var identity = new FakeAccountIdentity
        {
            FindByEmailResult = new AccountUser("user-1", "user@example.com"),
            IsEmailConfirmedResult = false,
            EmailConfirmationToken = string.Empty,
        };
        var emailSender = new FakeAccountEmailSender();
        using var services = AccountHandlerTestServices.Create(identity, emailSender);

        var handler = services.GetRequiredService<IResendConfirmationHandler>();
        var outcome = await handler.HandleAsync("user@example.com");

        Assert.Equal(ResendConfirmationOutcomeStatus.Completed, outcome.Status);
        Assert.False(emailSender.ConfirmationEmailSent);
    }

    [Fact]
    public async Task HandleAsync_completes_when_email_sender_fails()
    {
        var identity = new FakeAccountIdentity
        {
            FindByEmailResult = new AccountUser("user-1", "user@example.com"),
            IsEmailConfirmedResult = false,
            EmailConfirmationToken = "confirm-token",
        };
        var emailSender = new FakeAccountEmailSender
        {
            SendConfirmationException = new InvalidOperationException("mail failed"),
        };
        using var services = AccountHandlerTestServices.Create(identity, emailSender);

        var handler = services.GetRequiredService<IResendConfirmationHandler>();
        var outcome = await handler.HandleAsync("user@example.com");

        Assert.Equal(ResendConfirmationOutcomeStatus.Completed, outcome.Status);
        Assert.False(emailSender.ConfirmationEmailSent);
    }
}
