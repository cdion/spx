namespace Spx.Account.Application;

public interface IAccountEmailSender
{
    Task SendConfirmationEmailAsync(
        string email,
        string userId,
        string code,
        CancellationToken cancellationToken = default
    );

    Task SendPasswordResetEmailAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default
    );
}
