using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Spx.Web.Components;

internal static class UiHelpers
{
    public static string FormatDateTime(DateTime value)
        => value.ToLocalTime().ToString("g");

    public static async Task<string> GetUserIdAsync(Task<AuthenticationState> authenticationStateTask)
    {
        var authenticationState = await authenticationStateTask;
        return authenticationState.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("The current user does not have an identity id claim.");
    }
}
