using Spx.Account.Application;

namespace Spx.Account.Application.Features.ConfirmEmail;

public interface IConfirmEmailHandler
{
    Task<ConfirmEmailOutcome> HandleAsync(string userId, string code);
}
