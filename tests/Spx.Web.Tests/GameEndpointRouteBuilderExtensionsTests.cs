using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spx.Game.Application;
using Spx.Game.Application.Features.CreateGame;
using Spx.Game.Application.Features.JoinGame;
using Spx.Web.Endpoints;
using Xunit;

namespace Spx.Web.Tests;

public sealed class GameEndpointRouteBuilderExtensionsTests
{
    [Fact]
    public async Task Create_redirects_to_game_when_handler_succeeds()
    {
        await using var app = await CreateAppAsync(services =>
        {
            services.AddSingleton<ICreateGameHandler>(
                new StubCreateGameHandler(
                    new GameCommandSucceeded(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"))
                )
            );
            services.AddSingleton<IJoinGameHandler>(
                new StubJoinGameHandler(new GameCommandFailed("unused"))
            );
        });

        var client = app.GetTestClient();
        var response = await client.PostAsync(
            "/games/create",
            CreateFormContent(("gameName", "Weekend Match"), ("playerName", "Captain Red"))
        );

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            "/games/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            response.Headers.Location?.OriginalString
        );
    }

    [Fact]
    public async Task Create_redirects_back_with_error_and_values_when_handler_fails()
    {
        await using var app = await CreateAppAsync(services =>
        {
            services.AddSingleton<ICreateGameHandler>(
                new StubCreateGameHandler(
                    new GameCommandFailed("Game names must be at least 2 characters long.")
                )
            );
            services.AddSingleton<IJoinGameHandler>(
                new StubJoinGameHandler(new GameCommandFailed("unused"))
            );
        });

        var client = app.GetTestClient();
        var response = await client.PostAsync(
            "/games/create",
            CreateFormContent(("gameName", "A"), ("playerName", "Captain Red"))
        );

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            "/games/create?error=Game%20names%20must%20be%20at%20least%202%20characters%20long.&gameName=A&playerName=Captain%20Red",
            response.Headers.Location?.OriginalString
        );
    }

    [Fact]
    public async Task Join_redirects_back_with_error_and_values_when_handler_fails()
    {
        await using var app = await CreateAppAsync(services =>
        {
            services.AddSingleton<ICreateGameHandler>(
                new StubCreateGameHandler(new GameCommandFailed("unused"))
            );
            services.AddSingleton<IJoinGameHandler>(
                new StubJoinGameHandler(new GameCommandFailed("That invite code was not found."))
            );
        });

        var client = app.GetTestClient();
        var response = await client.PostAsync(
            "/games/join",
            CreateFormContent(("inviteCode", "ABC123"), ("playerName", "Captain Blue"))
        );

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            "/games/join?error=That%20invite%20code%20was%20not%20found.&inviteCode=ABC123&playerName=Captain%20Blue",
            response.Headers.Location?.OriginalString
        );
    }

    private static async Task<WebApplication> CreateAppAsync(
        Action<IServiceCollection> configureServices
    )
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder
            .Services.AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName,
                _ => { }
            );
        builder.Services.AddAuthorization();
        configureServices(builder.Services);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGameEndpoints();
        await app.StartAsync();
        return app;
    }

    private static FormUrlEncodedContent CreateFormContent(
        params (string Key, string Value)[] values
    ) => new(values.Select(static value => KeyValuePair.Create(value.Key, value.Value)));

    private sealed record StubCreateGameHandler(GameCommandOutcome Outcome) : ICreateGameHandler
    {
        public Task<GameCommandOutcome> HandleAsync(
            string userId,
            CreateGameRequest request,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(Outcome);
    }

    private sealed record StubJoinGameHandler(GameCommandOutcome Outcome) : IJoinGameHandler
    {
        public Task<GameCommandOutcome> HandleAsync(
            string userId,
            JoinGameRequest request,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(Outcome);
    }

    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder
    ) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Test";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var principal = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")], SchemeName)
            );
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
