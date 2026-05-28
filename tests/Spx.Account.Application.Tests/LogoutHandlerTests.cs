using Microsoft.Extensions.DependencyInjection;
using Spx.Account.Application.Features.Logout;
using Xunit;

namespace Spx.Account.Application.Tests;

public sealed class LogoutHandlerTests
{
    [Fact]
    public async Task HandleAsync_signs_out_current_user()
    {
        var identity = new FakeAccountIdentity();
        using var services = AccountHandlerTestServices.Create(identity);

        var handler = services.GetRequiredService<ILogoutHandler>();
        await handler.HandleAsync();

        Assert.True(identity.SignOutCalled);
    }
}
