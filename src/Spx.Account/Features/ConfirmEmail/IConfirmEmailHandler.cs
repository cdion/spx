using Spx.Account;

namespace Spx.Account.Features.ConfirmEmail;

public interface IConfirmEmailHandler
{
    Task<ConfirmEmailOutcome> HandleAsync(string userId, string code);
}