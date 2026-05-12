using Microsoft.AspNetCore.Http.Extensions;
using Spx.Account;
using Spx.Account.Features.ConfirmEmail;
using Spx.Account.Features.ForgotPassword;
using Spx.Account.Features.Login;
using Spx.Account.Features.Logout;
using Spx.Account.Features.Register;
using Spx.Account.Features.ResendConfirmation;
using Spx.Account.Features.ResetPassword;

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

    private static async Task<IResult> LoginAsync(HttpContext httpContext, ILoginHandler handler)
    {
        var form = await httpContext.Request.ReadFormAsync();
        var email = GetRequiredValue(form, "email");
        var password = GetRequiredValue(form, "password");
        return Results.LocalRedirect(MapLoginOutcome(await handler.HandleAsync(email, password, form["returnUrl"])));
    }

    private static async Task<IResult> LogoutAsync(ILogoutHandler handler)
    {
        await handler.HandleAsync();
        return Results.LocalRedirect(BuildRedirect("/login", ("status", "You have been signed out.")));
    }

    private static async Task<IResult> RegisterAsync(HttpContext httpContext, IRegisterHandler handler)
    {
        var form = await httpContext.Request.ReadFormAsync();
        var email = GetRequiredValue(form, "email");
        var password = GetRequiredValue(form, "password");
        var confirmPassword = GetRequiredValue(form, "confirmPassword");
        return Results.LocalRedirect(MapRegisterOutcome(await handler.HandleAsync(email, password, confirmPassword, httpContext.RequestAborted)));
    }

    private static async Task<IResult> ConfirmEmailAsync(IConfirmEmailHandler handler, string userId, string code)
    {
        return Results.LocalRedirect(MapConfirmEmailOutcome(await handler.HandleAsync(userId, code)));
    }

    private static async Task<IResult> ForgotPasswordAsync(HttpContext httpContext, IForgotPasswordHandler handler)
    {
        var form = await httpContext.Request.ReadFormAsync();
        var email = GetRequiredValue(form, "email");
        return Results.LocalRedirect(MapForgotPasswordOutcome(await handler.HandleAsync(email, httpContext.RequestAborted)));
    }

    private static async Task<IResult> ResetPasswordAsync(HttpContext httpContext, IResetPasswordHandler handler)
    {
        var form = await httpContext.Request.ReadFormAsync();
        var email = GetRequiredValue(form, "email");
        var code = GetRequiredValue(form, "code");
        var password = GetRequiredValue(form, "password");
        var confirmPassword = GetRequiredValue(form, "confirmPassword");
        return Results.LocalRedirect(MapResetPasswordOutcome(await handler.HandleAsync(email, code, password, confirmPassword)));
    }

    private static async Task<IResult> ResendConfirmationAsync(HttpContext httpContext, IResendConfirmationHandler handler)
    {
        var form = await httpContext.Request.ReadFormAsync();
        var email = GetRequiredValue(form, "email");
        return Results.LocalRedirect(MapResendConfirmationOutcome(await handler.HandleAsync(email, httpContext.RequestAborted)));
    }

    private static string MapLoginOutcome(LoginOutcome outcome)
        => outcome.Status switch
        {
            LoginOutcomeStatus.Succeeded => outcome.ReturnUrl,
            LoginOutcomeStatus.ValidationFailed => BuildRedirect("/login", ("error", "Email and password are required."), ("returnUrl", outcome.ReturnUrl)),
            LoginOutcomeStatus.InvalidCredentials => BuildRedirect("/login", ("error", "Invalid email or password."), ("returnUrl", outcome.ReturnUrl)),
            LoginOutcomeStatus.EmailConfirmationRequired => BuildRedirect("/login", ("error", "Confirm your email before signing in."), ("email", outcome.Email), ("returnUrl", outcome.ReturnUrl)),
            LoginOutcomeStatus.LockedOut => BuildRedirect("/login", ("error", "Your account is temporarily locked. Try again later."), ("returnUrl", outcome.ReturnUrl)),
            _ => BuildRedirect("/login", ("error", "Invalid email or password."), ("returnUrl", outcome.ReturnUrl))
        };

    private static string MapRegisterOutcome(RegisterOutcome outcome)
        => outcome.Status switch
        {
            RegisterOutcomeStatus.ValidationFailed => BuildRedirect("/register", ("error", "Email, password, and confirmation are required.")),
            RegisterOutcomeStatus.PasswordMismatch => BuildRedirect("/register", ("error", "Passwords do not match."), ("email", outcome.Email)),
            RegisterOutcomeStatus.Failed => BuildRedirect("/register", ("error", string.Join(" ", outcome.Errors ?? [])), ("email", outcome.Email)),
            RegisterOutcomeStatus.ConfirmationResendRequired => BuildRedirect("/resend-confirmation", ("status", string.Join(" ", outcome.Errors ?? [])), ("email", outcome.Email)),
            RegisterOutcomeStatus.ConfirmationSent => BuildRedirect("/login", ("status", "Check your email to confirm your account."), ("email", outcome.Email)),
            _ => BuildRedirect("/register", ("error", "Unable to register."), ("email", outcome.Email))
        };

    private static string MapConfirmEmailOutcome(ConfirmEmailOutcome outcome)
        => outcome.Status switch
        {
            ConfirmEmailOutcomeStatus.InvalidLink => BuildRedirect("/login", ("error", "The confirmation link is invalid.")),
            ConfirmEmailOutcomeStatus.Succeeded => BuildRedirect("/login", ("status", "Your email has been confirmed. You can sign in now."), ("email", outcome.Email)),
            ConfirmEmailOutcomeStatus.Failed => BuildRedirect("/login", ("error", "Email confirmation failed. Request a new confirmation email and try again."), ("email", outcome.Email)),
            _ => BuildRedirect("/login", ("error", "The confirmation link is invalid."))
        };

    private static string MapForgotPasswordOutcome(ForgotPasswordOutcome outcome)
        => outcome.Status switch
        {
            ForgotPasswordOutcomeStatus.ValidationFailed => BuildRedirect("/forgot-password", ("error", "Email is required.")),
            ForgotPasswordOutcomeStatus.Completed => BuildRedirect("/forgot-password", ("status", "If the account exists and the email is confirmed, a reset link has been sent.")),
            _ => BuildRedirect("/forgot-password", ("error", "Email is required."))
        };

    private static string MapResetPasswordOutcome(ResetPasswordOutcome outcome)
        => outcome.Status switch
        {
            ResetPasswordOutcomeStatus.IncompleteLink => BuildRedirect("/reset-password", ("error", "The reset link is incomplete."), ("email", outcome.Email), ("code", outcome.Code)),
            ResetPasswordOutcomeStatus.PasswordMismatch => BuildRedirect("/reset-password", ("error", "Passwords do not match."), ("email", outcome.Email), ("code", outcome.Code)),
            ResetPasswordOutcomeStatus.Completed => BuildRedirect("/login", ("status", "Your password has been reset. You can sign in now."), ("email", outcome.Email)),
            ResetPasswordOutcomeStatus.Failed => BuildRedirect("/reset-password", ("error", string.Join(" ", outcome.Errors ?? [])), ("email", outcome.Email), ("code", outcome.Code)),
            _ => BuildRedirect("/reset-password", ("error", "The reset link is incomplete."), ("email", outcome.Email), ("code", outcome.Code))
        };

    private static string MapResendConfirmationOutcome(ResendConfirmationOutcome outcome)
        => outcome.Status switch
        {
            ResendConfirmationOutcomeStatus.ValidationFailed => BuildRedirect("/resend-confirmation", ("error", "Email is required.")),
            ResendConfirmationOutcomeStatus.Completed => BuildRedirect("/resend-confirmation", ("status", "If the account exists and is still unconfirmed, a new confirmation email has been sent."), ("email", outcome.Email)),
            _ => BuildRedirect("/resend-confirmation", ("error", "Email is required."))
        };

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

    private static string GetRequiredValue(IFormCollection form, string key)
        => form.TryGetValue(key, out var value) ? value.ToString().Trim() : string.Empty;
}