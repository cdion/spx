using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Spx.Account.Application;
using Spx.Account.Application.Features.ConfirmEmail;
using Spx.Account.Application.Features.ForgotPassword;
using Spx.Account.Application.Features.Login;
using Spx.Account.Application.Features.Logout;
using Spx.Account.Application.Features.Register;
using Spx.Account.Application.Features.ResendConfirmation;
using Spx.Account.Application.Features.ResetPassword;
using Spx.Web.Endpoints;
using Xunit;

namespace Spx.Web.Tests;

public sealed class AccountEndpointRouteBuilderExtensionsTests
{
    [Fact]
    public async Task Login_redirects_to_return_url_on_success()
    {
        await using var app = await CreateAppAsync(services =>
        {
            services.AddSingleton<ILoginHandler>(
                new StubLoginHandler(new LoginOutcome(LoginOutcomeStatus.Succeeded, "/games"))
            );
            services.AddSingleton<ILogoutHandler>(new StubLogoutHandler());
            services.AddSingleton<IRegisterHandler>(
                new StubRegisterHandler(new RegisterOutcome(RegisterOutcomeStatus.ValidationFailed))
            );
            services.AddSingleton<IConfirmEmailHandler>(
                new StubConfirmEmailHandler(
                    new ConfirmEmailOutcome(ConfirmEmailOutcomeStatus.InvalidLink)
                )
            );
            services.AddSingleton<IForgotPasswordHandler>(
                new StubForgotPasswordHandler(
                    new ForgotPasswordOutcome(ForgotPasswordOutcomeStatus.ValidationFailed)
                )
            );
            services.AddSingleton<IResetPasswordHandler>(
                new StubResetPasswordHandler(
                    new ResetPasswordOutcome(
                        ResetPasswordOutcomeStatus.IncompleteLink,
                        string.Empty,
                        string.Empty
                    )
                )
            );
            services.AddSingleton<IResendConfirmationHandler>(
                new StubResendConfirmationHandler(
                    new ResendConfirmationOutcome(ResendConfirmationOutcomeStatus.ValidationFailed)
                )
            );
        });

        var client = app.GetTestClient();
        var response = await client.PostAsync(
            "/account/login",
            CreateFormContent(
                ("email", "user@example.com"),
                ("password", "secret"),
                ("returnUrl", "/games")
            )
        );

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/games", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Login_redirects_with_error_when_validation_fails()
    {
        await using var app = await CreateAppAsync(services =>
        {
            services.AddSingleton<ILoginHandler>(
                new StubLoginHandler(new LoginOutcome(LoginOutcomeStatus.ValidationFailed, "/"))
            );
            services.AddSingleton<ILogoutHandler>(new StubLogoutHandler());
            services.AddSingleton<IRegisterHandler>(
                new StubRegisterHandler(new RegisterOutcome(RegisterOutcomeStatus.ValidationFailed))
            );
            services.AddSingleton<IConfirmEmailHandler>(
                new StubConfirmEmailHandler(
                    new ConfirmEmailOutcome(ConfirmEmailOutcomeStatus.InvalidLink)
                )
            );
            services.AddSingleton<IForgotPasswordHandler>(
                new StubForgotPasswordHandler(
                    new ForgotPasswordOutcome(ForgotPasswordOutcomeStatus.ValidationFailed)
                )
            );
            services.AddSingleton<IResetPasswordHandler>(
                new StubResetPasswordHandler(
                    new ResetPasswordOutcome(
                        ResetPasswordOutcomeStatus.IncompleteLink,
                        string.Empty,
                        string.Empty
                    )
                )
            );
            services.AddSingleton<IResendConfirmationHandler>(
                new StubResendConfirmationHandler(
                    new ResendConfirmationOutcome(ResendConfirmationOutcomeStatus.ValidationFailed)
                )
            );
        });

        var client = app.GetTestClient();
        var response = await client.PostAsync(
            "/account/login",
            CreateFormContent(("email", string.Empty), ("password", string.Empty))
        );

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            "/login?error=Email%20and%20password%20are%20required.&returnUrl=%2F",
            response.Headers.Location?.OriginalString
        );
    }

    [Fact]
    public async Task Register_redirects_to_login_when_confirmation_is_sent()
    {
        await using var app = await CreateAppAsync(services =>
        {
            services.AddSingleton<ILoginHandler>(
                new StubLoginHandler(new LoginOutcome(LoginOutcomeStatus.ValidationFailed, "/"))
            );
            services.AddSingleton<ILogoutHandler>(new StubLogoutHandler());
            services.AddSingleton<IRegisterHandler>(
                new StubRegisterHandler(
                    new RegisterOutcome(RegisterOutcomeStatus.ConfirmationSent, "user@example.com")
                )
            );
            services.AddSingleton<IConfirmEmailHandler>(
                new StubConfirmEmailHandler(
                    new ConfirmEmailOutcome(ConfirmEmailOutcomeStatus.InvalidLink)
                )
            );
            services.AddSingleton<IForgotPasswordHandler>(
                new StubForgotPasswordHandler(
                    new ForgotPasswordOutcome(ForgotPasswordOutcomeStatus.ValidationFailed)
                )
            );
            services.AddSingleton<IResetPasswordHandler>(
                new StubResetPasswordHandler(
                    new ResetPasswordOutcome(
                        ResetPasswordOutcomeStatus.IncompleteLink,
                        string.Empty,
                        string.Empty
                    )
                )
            );
            services.AddSingleton<IResendConfirmationHandler>(
                new StubResendConfirmationHandler(
                    new ResendConfirmationOutcome(ResendConfirmationOutcomeStatus.ValidationFailed)
                )
            );
        });

        var client = app.GetTestClient();
        var response = await client.PostAsync(
            "/account/register",
            CreateFormContent(
                ("email", "user@example.com"),
                ("password", "Passw0rd!"),
                ("confirmPassword", "Passw0rd!")
            )
        );

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            "/login?status=Check%20your%20email%20to%20confirm%20your%20account.&email=user@example.com",
            response.Headers.Location?.OriginalString
        );
    }

    [Fact]
    public async Task Register_redirects_to_resend_confirmation_when_account_was_created_but_email_was_not_sent()
    {
        await using var app = await CreateAppAsync(services =>
        {
            services.AddSingleton<ILoginHandler>(
                new StubLoginHandler(new LoginOutcome(LoginOutcomeStatus.ValidationFailed, "/"))
            );
            services.AddSingleton<ILogoutHandler>(new StubLogoutHandler());
            services.AddSingleton<IRegisterHandler>(
                new StubRegisterHandler(
                    new RegisterOutcome(
                        RegisterOutcomeStatus.ConfirmationResendRequired,
                        "user@example.com",
                        [
                            "Your account was created, but we could not send a confirmation email. Request a new one below.",
                        ]
                    )
                )
            );
            services.AddSingleton<IConfirmEmailHandler>(
                new StubConfirmEmailHandler(
                    new ConfirmEmailOutcome(ConfirmEmailOutcomeStatus.InvalidLink)
                )
            );
            services.AddSingleton<IForgotPasswordHandler>(
                new StubForgotPasswordHandler(
                    new ForgotPasswordOutcome(ForgotPasswordOutcomeStatus.ValidationFailed)
                )
            );
            services.AddSingleton<IResetPasswordHandler>(
                new StubResetPasswordHandler(
                    new ResetPasswordOutcome(
                        ResetPasswordOutcomeStatus.IncompleteLink,
                        string.Empty,
                        string.Empty
                    )
                )
            );
            services.AddSingleton<IResendConfirmationHandler>(
                new StubResendConfirmationHandler(
                    new ResendConfirmationOutcome(ResendConfirmationOutcomeStatus.ValidationFailed)
                )
            );
        });

        var client = app.GetTestClient();
        var response = await client.PostAsync(
            "/account/register",
            CreateFormContent(
                ("email", "user@example.com"),
                ("password", "Passw0rd!"),
                ("confirmPassword", "Passw0rd!")
            )
        );

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            "/resend-confirmation?status=Your%20account%20was%20created,%20but%20we%20could%20not%20send%20a%20confirmation%20email.%20Request%20a%20new%20one%20below.&email=user@example.com",
            response.Headers.Location?.OriginalString
        );
    }

    [Fact]
    public async Task Reset_password_redirects_back_to_form_on_failure()
    {
        await using var app = await CreateAppAsync(services =>
        {
            services.AddSingleton<ILoginHandler>(
                new StubLoginHandler(new LoginOutcome(LoginOutcomeStatus.ValidationFailed, "/"))
            );
            services.AddSingleton<ILogoutHandler>(new StubLogoutHandler());
            services.AddSingleton<IRegisterHandler>(
                new StubRegisterHandler(new RegisterOutcome(RegisterOutcomeStatus.ValidationFailed))
            );
            services.AddSingleton<IConfirmEmailHandler>(
                new StubConfirmEmailHandler(
                    new ConfirmEmailOutcome(ConfirmEmailOutcomeStatus.InvalidLink)
                )
            );
            services.AddSingleton<IForgotPasswordHandler>(
                new StubForgotPasswordHandler(
                    new ForgotPasswordOutcome(ForgotPasswordOutcomeStatus.ValidationFailed)
                )
            );
            services.AddSingleton<IResetPasswordHandler>(
                new StubResetPasswordHandler(
                    new ResetPasswordOutcome(
                        ResetPasswordOutcomeStatus.Failed,
                        "user@example.com",
                        "abc",
                        ["Reset failed."]
                    )
                )
            );
            services.AddSingleton<IResendConfirmationHandler>(
                new StubResendConfirmationHandler(
                    new ResendConfirmationOutcome(ResendConfirmationOutcomeStatus.ValidationFailed)
                )
            );
        });

        var client = app.GetTestClient();
        var response = await client.PostAsync(
            "/account/reset-password",
            CreateFormContent(
                ("email", "user@example.com"),
                ("code", "abc"),
                ("password", "new-password"),
                ("confirmPassword", "new-password")
            )
        );

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            "/reset-password?error=Reset%20failed.&email=user@example.com&code=abc",
            response.Headers.Location?.OriginalString
        );
    }

    private static async Task<WebApplication> CreateAppAsync(
        Action<IServiceCollection> configureServices
    )
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        configureServices(builder.Services);

        var app = builder.Build();
        app.MapAccountEndpoints();
        await app.StartAsync();
        return app;
    }

    private static FormUrlEncodedContent CreateFormContent(
        params (string Key, string Value)[] values
    ) => new(values.Select(static value => KeyValuePair.Create(value.Key, value.Value)));

    private sealed record StubLoginHandler(LoginOutcome Outcome) : ILoginHandler
    {
        public Task<LoginOutcome> HandleAsync(string email, string password, string? returnUrl) =>
            Task.FromResult(Outcome);
    }

    private sealed class StubLogoutHandler : ILogoutHandler
    {
        public Task<LogoutOutcome> HandleAsync() => Task.FromResult(new LogoutOutcome());
    }

    private sealed record StubRegisterHandler(RegisterOutcome Outcome) : IRegisterHandler
    {
        public Task<RegisterOutcome> HandleAsync(
            string email,
            string password,
            string confirmPassword,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(Outcome);
    }

    private sealed record StubConfirmEmailHandler(ConfirmEmailOutcome Outcome)
        : IConfirmEmailHandler
    {
        public Task<ConfirmEmailOutcome> HandleAsync(string userId, string code) =>
            Task.FromResult(Outcome);
    }

    private sealed record StubForgotPasswordHandler(ForgotPasswordOutcome Outcome)
        : IForgotPasswordHandler
    {
        public Task<ForgotPasswordOutcome> HandleAsync(
            string email,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(Outcome);
    }

    private sealed record StubResetPasswordHandler(ResetPasswordOutcome Outcome)
        : IResetPasswordHandler
    {
        public Task<ResetPasswordOutcome> HandleAsync(
            string email,
            string code,
            string password,
            string confirmPassword
        ) => Task.FromResult(Outcome);
    }

    private sealed record StubResendConfirmationHandler(ResendConfirmationOutcome Outcome)
        : IResendConfirmationHandler
    {
        public Task<ResendConfirmationOutcome> HandleAsync(
            string email,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(Outcome);
    }
}
