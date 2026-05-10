namespace Spx.Web.Services.Email;

public interface IAccountEmailSender
{
    Task SendConfirmationLinkAsync(string email, string confirmationLink, CancellationToken cancellationToken = default);

    Task SendPasswordResetLinkAsync(string email, string resetLink, CancellationToken cancellationToken = default);
}