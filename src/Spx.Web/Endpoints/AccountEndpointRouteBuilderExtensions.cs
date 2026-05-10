using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Spx.Data;
using Spx.Web.Services.Email;

namespace Spx.Web.Endpoints;

public static class AccountEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/account");

        group.MapPost("/login", LoginAsync);
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();
        group.MapPost("/register", RegisterAsync);
        group.MapGet("/confirm-email", ConfirmEmailAsync);
        group.MapPost("/forgot-password", ForgotPasswordAsync);
        group.MapPost("/reset-password", ResetPasswordAsync);
        group.MapPost("/resend-confirmation", ResendConfirmationAsync);

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        HttpContext httpContext,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        var form = await httpContext.Request.ReadFormAsync();
        var email = GetRequiredValue(form, "email");
        var password = GetRequiredValue(form, "password");
        var returnUrl = GetLocalReturnUrl(form["returnUrl"]);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return Results.LocalRedirect(BuildRedirect("/login", ("error", "Email and password are required."), ("returnUrl", returnUrl)));
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Results.LocalRedirect(BuildRedirect("/login", ("error", "Invalid email or password."), ("returnUrl", returnUrl)));
        }

        var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return Results.LocalRedirect(returnUrl);
        }

        if (result.IsNotAllowed)
        {
            return Results.LocalRedirect(BuildRedirect("/login", ("error", "Confirm your email before signing in."), ("email", email), ("returnUrl", returnUrl)));
        }

        if (result.IsLockedOut)
        {
            return Results.LocalRedirect(BuildRedirect("/login", ("error", "Your account is temporarily locked. Try again later."), ("returnUrl", returnUrl)));
        }

        return Results.LocalRedirect(BuildRedirect("/login", ("error", "Invalid email or password."), ("returnUrl", returnUrl)));
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext, SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.LocalRedirect("/login?status=You%20have%20been%20signed%20out.");
    }

    private static async Task<IResult> RegisterAsync(
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        IAccountEmailSender emailSender)
    {
        var form = await httpContext.Request.ReadFormAsync();
        var email = GetRequiredValue(form, "email");
        var password = GetRequiredValue(form, "password");
        var confirmPassword = GetRequiredValue(form, "confirmPassword");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
        {
            return Results.LocalRedirect(BuildRedirect("/register", ("error", "Email, password, and confirmation are required.")));
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            return Results.LocalRedirect(BuildRedirect("/register", ("error", "Passwords do not match."), ("email", email)));
        }

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return Results.LocalRedirect(BuildRedirect("/register", ("error", string.Join(" ", result.Errors.Select(static error => error.Description))), ("email", email)));
        }

        var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink = BuildAbsoluteUri(httpContext, "/account/confirm-email", ("userId", user.Id), ("code", code));
        await emailSender.SendConfirmationLinkAsync(email, confirmationLink, httpContext.RequestAborted);

        return Results.LocalRedirect(BuildRedirect("/login", ("status", "Check your email to confirm your account."), ("email", email)));
    }

    private static async Task<IResult> ConfirmEmailAsync(
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        string userId,
        string code)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
        {
            return Results.LocalRedirect(BuildRedirect("/login", ("error", "The confirmation link is invalid.")));
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Results.LocalRedirect(BuildRedirect("/login", ("error", "The confirmation link is invalid.")));
        }

        var result = await userManager.ConfirmEmailAsync(user, code);
        return result.Succeeded
            ? Results.LocalRedirect(BuildRedirect("/login", ("status", "Your email has been confirmed. You can sign in now."), ("email", user.Email)))
            : Results.LocalRedirect(BuildRedirect("/login", ("error", "Email confirmation failed. Request a new confirmation email and try again."), ("email", user.Email)));
    }

    private static async Task<IResult> ForgotPasswordAsync(
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        IAccountEmailSender emailSender)
    {
        var form = await httpContext.Request.ReadFormAsync();
        var email = GetRequiredValue(form, "email");
        var successMessage = "If the account exists and the email is confirmed, a reset link has been sent.";

        if (string.IsNullOrWhiteSpace(email))
        {
            return Results.LocalRedirect(BuildRedirect("/forgot-password", ("error", "Email is required.")));
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is not null && await userManager.IsEmailConfirmedAsync(user))
        {
            var code = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = BuildAbsoluteUri(httpContext, "/reset-password", ("email", email), ("code", code));
            await emailSender.SendPasswordResetLinkAsync(email, resetLink, httpContext.RequestAborted);
        }

        return Results.LocalRedirect(BuildRedirect("/forgot-password", ("status", successMessage)));
    }

    private static async Task<IResult> ResetPasswordAsync(
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager)
    {
        var form = await httpContext.Request.ReadFormAsync();
        var email = GetRequiredValue(form, "email");
        var code = GetRequiredValue(form, "code");
        var password = GetRequiredValue(form, "password");
        var confirmPassword = GetRequiredValue(form, "confirmPassword");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
        {
            return Results.LocalRedirect(BuildRedirect("/reset-password", ("error", "The reset link is incomplete."), ("email", email), ("code", code)));
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            return Results.LocalRedirect(BuildRedirect("/reset-password", ("error", "Passwords do not match."), ("email", email), ("code", code)));
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Results.LocalRedirect(BuildRedirect("/login", ("status", "Your password has been reset. You can sign in now."), ("email", email)));
        }

        var result = await userManager.ResetPasswordAsync(user, code, password);
        return result.Succeeded
            ? Results.LocalRedirect(BuildRedirect("/login", ("status", "Your password has been reset. You can sign in now."), ("email", email)))
            : Results.LocalRedirect(BuildRedirect("/reset-password", ("error", string.Join(" ", result.Errors.Select(static error => error.Description))), ("email", email), ("code", code)));
    }

    private static async Task<IResult> ResendConfirmationAsync(
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        IAccountEmailSender emailSender)
    {
        var form = await httpContext.Request.ReadFormAsync();
        var email = GetRequiredValue(form, "email");

        if (string.IsNullOrWhiteSpace(email))
        {
            return Results.LocalRedirect(BuildRedirect("/resend-confirmation", ("error", "Email is required.")));
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is not null && !await userManager.IsEmailConfirmedAsync(user))
        {
            var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = BuildAbsoluteUri(httpContext, "/account/confirm-email", ("userId", user.Id), ("code", code));
            await emailSender.SendConfirmationLinkAsync(email, confirmationLink, httpContext.RequestAborted);
        }

        return Results.LocalRedirect(BuildRedirect("/resend-confirmation", ("status", "If the account exists and is still unconfirmed, a new confirmation email has been sent."), ("email", email)));
    }

    private static string GetRequiredValue(IFormCollection form, string key)
        => form.TryGetValue(key, out var value) ? value.ToString().Trim() : string.Empty;

    private static string GetLocalReturnUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//", StringComparison.Ordinal))
        {
            return returnUrl;
        }

        return "/";
    }

    private static string BuildRedirect(string path, params (string Key, string? Value)[] values)
    {
        var queryBuilder = new QueryBuilder();

        foreach (var (key, value) in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                queryBuilder.Add(key, value);
            }
        }

        return $"{path}{queryBuilder}";
    }

    private static string BuildAbsoluteUri(HttpContext httpContext, string path, params (string Key, string? Value)[] values)
    {
        var query = new QueryBuilder();

        foreach (var (key, value) in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                query.Add(key, value);
            }
        }

        return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{path}{query}";
    }
}