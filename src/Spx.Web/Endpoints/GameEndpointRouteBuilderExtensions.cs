using System.Security.Claims;
using Microsoft.AspNetCore.Http.Extensions;
using Spx.Game.Application;
using Spx.Game.Application.Features.CreateGame;
using Spx.Game.Application.Features.JoinGame;

namespace Spx.Web.Endpoints;

public static partial class GameEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/games").RequireAuthorization();

        group.MapPost("/create/submit", CreateGameAsync);
        group.MapPost("/join/submit", JoinGameAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateGameAsync(
        HttpContext httpContext,
        ICreateGameHandler handler,
        ILoggerFactory loggerFactory
    )
    {
        var form = await httpContext.Request.ReadFormAsync();
        var gameName = GetRequiredValue(form, "gameName");
        var playerName = GetRequiredValue(form, "playerName");
        var userId = GetUserId(httpContext.User);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.LocalRedirect(BuildRedirect("/login", ("returnUrl", "/games/create")));
        }

        try
        {
            var result = await handler.HandleAsync(
                userId,
                new CreateGameRequest(gameName, playerName),
                httpContext.RequestAborted
            );

            return result switch
            {
                GameCommandSucceeded succeeded => Results.LocalRedirect(
                    $"/games/{succeeded.GameId}"
                ),
                GameCommandFailed failed => Results.LocalRedirect(
                    BuildRedirect(
                        "/games/create",
                        ("error", failed.ErrorMessage),
                        ("gameName", gameName),
                        ("playerName", playerName)
                    )
                ),
                _ => Results.LocalRedirect(
                    BuildRedirect(
                        "/games/create",
                        ("error", "We couldn't create a game right now. Please try again."),
                        ("gameName", gameName),
                        ("playerName", playerName)
                    )
                ),
            };
        }
        catch (Exception exception)
        {
            var logger = loggerFactory.CreateLogger(typeof(GameEndpointRouteBuilderExtensions));
            LogCreateGameFailed(logger, exception);
            return Results.LocalRedirect(
                BuildRedirect(
                    "/games/create",
                    ("error", "We couldn't create a game right now. Please try again."),
                    ("gameName", gameName),
                    ("playerName", playerName)
                )
            );
        }
    }

    private static async Task<IResult> JoinGameAsync(
        HttpContext httpContext,
        IJoinGameHandler handler,
        ILoggerFactory loggerFactory
    )
    {
        var form = await httpContext.Request.ReadFormAsync();
        var inviteCode = GetRequiredValue(form, "inviteCode");
        var playerName = GetRequiredValue(form, "playerName");
        var userId = GetUserId(httpContext.User);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.LocalRedirect(BuildRedirect("/login", ("returnUrl", "/games/join")));
        }

        try
        {
            var result = await handler.HandleAsync(
                userId,
                new JoinGameRequest(inviteCode, playerName),
                httpContext.RequestAborted
            );

            return result switch
            {
                GameCommandSucceeded succeeded => Results.LocalRedirect(
                    $"/games/{succeeded.GameId}"
                ),
                GameCommandFailed failed => Results.LocalRedirect(
                    BuildRedirect(
                        "/games/join",
                        ("error", failed.ErrorMessage),
                        ("inviteCode", inviteCode),
                        ("playerName", playerName)
                    )
                ),
                _ => Results.LocalRedirect(
                    BuildRedirect(
                        "/games/join",
                        ("error", "We couldn't join that game right now. Please try again."),
                        ("inviteCode", inviteCode),
                        ("playerName", playerName)
                    )
                ),
            };
        }
        catch (Exception exception)
        {
            var logger = loggerFactory.CreateLogger(typeof(GameEndpointRouteBuilderExtensions));
            LogJoinGameFailed(logger, exception);
            return Results.LocalRedirect(
                BuildRedirect(
                    "/games/join",
                    ("error", "We couldn't join that game right now. Please try again."),
                    ("inviteCode", inviteCode),
                    ("playerName", playerName)
                )
            );
        }
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to create a game for the current user."
    )]
    private static partial void LogCreateGameFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to join a game for the current user.")]
    private static partial void LogJoinGameFailed(ILogger logger, Exception exception);

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

    private static string GetRequiredValue(IFormCollection form, string key) =>
        form.TryGetValue(key, out var value) ? value.ToString().Trim() : string.Empty;

    private static string? GetUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier);
}
