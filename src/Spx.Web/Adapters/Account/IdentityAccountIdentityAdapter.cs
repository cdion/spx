using Microsoft.AspNetCore.Identity;
using Spx.Account;
using Spx.Data;

namespace Spx.Web.Adapters.Account;

internal sealed class IdentityAccountIdentityAdapter(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : IAccountIdentity
{
    public async Task<AccountUser?> FindByEmailAsync(string email)
        => Map(await userManager.FindByEmailAsync(email));

    public async Task<AccountUser?> FindByIdAsync(string userId)
        => Map(await userManager.FindByIdAsync(userId));

    public async Task<AccountPasswordSignInOutcome> PasswordSignInAsync(AccountUser user, string password)
    {
        var applicationUser = await userManager.FindByIdAsync(user.Id);
        if (applicationUser is null)
        {
            return new AccountPasswordSignInOutcome(AccountPasswordSignInStatus.Failed);
        }

        var result = await signInManager.PasswordSignInAsync(applicationUser, password, isPersistent: false, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return new AccountPasswordSignInOutcome(AccountPasswordSignInStatus.Succeeded);
        }

        if (result.IsNotAllowed)
        {
            return new AccountPasswordSignInOutcome(AccountPasswordSignInStatus.EmailConfirmationRequired);
        }

        if (result.IsLockedOut)
        {
            return new AccountPasswordSignInOutcome(AccountPasswordSignInStatus.LockedOut);
        }

        return new AccountPasswordSignInOutcome(AccountPasswordSignInStatus.Failed);
    }

    public Task SignOutAsync()
        => signInManager.SignOutAsync();

    public async Task<AccountCreateOutcome> CreateUserAsync(string email, string password)
    {
        var user = new ApplicationUser
        {
            Email = email,
            UserName = email
        };

        var result = await userManager.CreateAsync(user, password);
        return result.Succeeded
            ? new AccountCreateSucceeded(Map(user)!)
            : new AccountCreateFailed(MapErrors(result));
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(AccountUser user)
    {
        var applicationUser = await FindUserAsync(user.Id);
        return applicationUser is null ? string.Empty : await userManager.GenerateEmailConfirmationTokenAsync(applicationUser);
    }

    public async Task<AccountOperationOutcome> ConfirmEmailAsync(AccountUser user, string code)
    {
        var applicationUser = await FindUserAsync(user.Id);
        return applicationUser is null
            ? Failure("Account user could not be found.")
            : Map(await userManager.ConfirmEmailAsync(applicationUser, code));
    }

    public async Task<bool> IsEmailConfirmedAsync(AccountUser user)
    {
        var applicationUser = await FindUserAsync(user.Id);
        return applicationUser is not null && await userManager.IsEmailConfirmedAsync(applicationUser);
    }

    public async Task<string> GeneratePasswordResetTokenAsync(AccountUser user)
    {
        var applicationUser = await FindUserAsync(user.Id);
        return applicationUser is null ? string.Empty : await userManager.GeneratePasswordResetTokenAsync(applicationUser);
    }

    public async Task<AccountOperationOutcome> ResetPasswordAsync(AccountUser user, string code, string password)
    {
        var applicationUser = await FindUserAsync(user.Id);
        return applicationUser is null
            ? Failure("Account user could not be found.")
            : Map(await userManager.ResetPasswordAsync(applicationUser, code, password));
    }

    private Task<ApplicationUser?> FindUserAsync(string userId)
        => userManager.FindByIdAsync(userId);

    private static AccountUser? Map(ApplicationUser? user)
        => user is null ? null : new AccountUser(user.Id, user.Email ?? string.Empty);

    private static AccountOperationOutcome Map(IdentityResult result)
        => result.Succeeded ? new AccountOperationSucceeded() : new AccountOperationFailed(MapErrors(result));

    private static AccountOperationOutcome Failure(params string[] errors)
        => new AccountOperationFailed(errors);

    private static string[] MapErrors(IdentityResult result)
        => result.Errors.Select(static error => error.Description).ToArray();
}