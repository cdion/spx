namespace Spx.Account.Application;

public interface IAccountIdentity
{
    Task<AccountUser?> FindByEmailAsync(string email);

    Task<AccountUser?> FindByIdAsync(string userId);

    Task<AccountPasswordSignInOutcome> PasswordSignInAsync(AccountUser user, string password);

    Task SignOutAsync();

    Task<AccountCreateOutcome> CreateUserAsync(string email, string password);

    Task<string> GenerateEmailConfirmationTokenAsync(AccountUser user);

    Task<AccountOperationOutcome> ConfirmEmailAsync(AccountUser user, string code);

    Task<bool> IsEmailConfirmedAsync(AccountUser user);

    Task<string> GeneratePasswordResetTokenAsync(AccountUser user);

    Task<AccountOperationOutcome> ResetPasswordAsync(
        AccountUser user,
        string code,
        string password
    );
}

public sealed record AccountUser(string Id, string Email);

public enum AccountPasswordSignInStatus
{
    Succeeded,
    EmailConfirmationRequired,
    LockedOut,
    Failed,
}

public sealed record AccountPasswordSignInOutcome(AccountPasswordSignInStatus Status);

public abstract record AccountCreateOutcome;

public sealed record AccountCreateSucceeded(AccountUser User) : AccountCreateOutcome;

public sealed record AccountCreateFailed(IReadOnlyList<string> Errors) : AccountCreateOutcome;

public abstract record AccountOperationOutcome;

public sealed record AccountOperationSucceeded : AccountOperationOutcome;

public sealed record AccountOperationFailed(IReadOnlyList<string> Errors) : AccountOperationOutcome;
