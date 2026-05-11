namespace Spx.Account;

public interface IAccountIdentity
{
    Task<AccountUser?> FindByEmailAsync(string email);

    Task<AccountUser?> FindByIdAsync(string userId);

    Task<AccountPasswordSignInResult> PasswordSignInAsync(AccountUser user, string password);

    Task SignOutAsync();

    Task<AccountCreateResult> CreateUserAsync(string email, string password);

    Task<string> GenerateEmailConfirmationTokenAsync(AccountUser user);

    Task<AccountOperationResult> ConfirmEmailAsync(AccountUser user, string code);

    Task<bool> IsEmailConfirmedAsync(AccountUser user);

    Task<string> GeneratePasswordResetTokenAsync(AccountUser user);

    Task<AccountOperationResult> ResetPasswordAsync(AccountUser user, string code, string password);
}

public sealed record AccountUser(string Id, string Email);

public enum AccountPasswordSignInStatus
{
    Succeeded,
    EmailConfirmationRequired,
    LockedOut,
    Failed
}

public sealed record AccountPasswordSignInResult(AccountPasswordSignInStatus Status);

public sealed record AccountCreateResult(AccountUser? User, bool Succeeded, IReadOnlyList<string> Errors);

public sealed record AccountOperationResult(bool Succeeded, IReadOnlyList<string> Errors);