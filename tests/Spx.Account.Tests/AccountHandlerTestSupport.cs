using Microsoft.Extensions.DependencyInjection;
using Spx.Account;

namespace Spx.Account.Tests;

internal static class AccountHandlerTestServices
{
    public static ServiceProvider Create(FakeAccountIdentity identity, FakeAccountEmailSender? emailSender = null)
    {
        var services = new ServiceCollection();
        services.AddAccountApplication();
        services.AddSingleton<IAccountIdentity>(identity);
        services.AddSingleton<IAccountEmailSender>(emailSender ?? new FakeAccountEmailSender());
        return services.BuildServiceProvider();
    }
}

internal sealed class FakeAccountIdentity : IAccountIdentity
{
    public AccountUser? FindByEmailResult { get; init; }

    public AccountUser? FindByIdResult { get; init; }

    public AccountPasswordSignInResult PasswordSignInResult { get; init; } = new(AccountPasswordSignInStatus.Failed);

    public AccountCreateResult CreateUserResult { get; init; } = new(null, false, []);

    public string EmailConfirmationToken { get; init; } = string.Empty;

    public AccountOperationResult ConfirmEmailResult { get; init; } = new(false, []);

    public bool IsEmailConfirmedResult { get; init; }

    public string PasswordResetToken { get; init; } = string.Empty;

    public AccountOperationResult ResetPasswordResult { get; init; } = new(false, []);

    public bool SignOutCalled { get; private set; }

    public Task<AccountUser?> FindByEmailAsync(string email)
        => Task.FromResult(FindByEmailResult);

    public Task<AccountUser?> FindByIdAsync(string userId)
        => Task.FromResult(FindByIdResult);

    public Task<AccountPasswordSignInResult> PasswordSignInAsync(AccountUser user, string password)
        => Task.FromResult(PasswordSignInResult);

    public Task SignOutAsync()
    {
        SignOutCalled = true;
        return Task.CompletedTask;
    }

    public Task<AccountCreateResult> CreateUserAsync(string email, string password)
        => Task.FromResult(CreateUserResult);

    public Task<string> GenerateEmailConfirmationTokenAsync(AccountUser user)
        => Task.FromResult(EmailConfirmationToken);

    public Task<AccountOperationResult> ConfirmEmailAsync(AccountUser user, string code)
        => Task.FromResult(ConfirmEmailResult);

    public Task<bool> IsEmailConfirmedAsync(AccountUser user)
        => Task.FromResult(IsEmailConfirmedResult);

    public Task<string> GeneratePasswordResetTokenAsync(AccountUser user)
        => Task.FromResult(PasswordResetToken);

    public Task<AccountOperationResult> ResetPasswordAsync(AccountUser user, string code, string password)
        => Task.FromResult(ResetPasswordResult);
}

internal sealed class FakeAccountEmailSender : IAccountEmailSender
{
    public bool ConfirmationEmailSent { get; private set; }

    public bool PasswordResetEmailSent { get; private set; }

    public string? LastConfirmationEmail { get; private set; }

    public string? LastConfirmationUserId { get; private set; }

    public string? LastConfirmationCode { get; private set; }

    public string? LastPasswordResetEmail { get; private set; }

    public string? LastPasswordResetCode { get; private set; }

    public Task SendConfirmationEmailAsync(string email, string userId, string code, CancellationToken cancellationToken = default)
    {
        ConfirmationEmailSent = true;
        LastConfirmationEmail = email;
        LastConfirmationUserId = userId;
        LastConfirmationCode = code;
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string email, string code, CancellationToken cancellationToken = default)
    {
        PasswordResetEmailSent = true;
        LastPasswordResetEmail = email;
        LastPasswordResetCode = code;
        return Task.CompletedTask;
    }
}