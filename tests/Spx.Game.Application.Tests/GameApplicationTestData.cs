using Spx.Game.Application;

namespace Spx.Game.Application.Tests;

internal static class GameApplicationTestData
{
    public static readonly Guid CurrentPlayerId = Guid.Parse(
        "4a4a0001-0000-0000-0000-000000000001"
    );
    public static readonly Guid OpponentPlayerId = Guid.Parse(
        "4a4a0002-0000-0000-0000-000000000002"
    );

    public static GamePlayerView CurrentPlayer() =>
        new(CurrentPlayerId, "Captain Red", DateTime.UtcNow);

    public static GamePlayerView OpponentPlayer() =>
        new(OpponentPlayerId, "Captain Blue", DateTime.UtcNow);

    public static GameLobbyView CreateLobby(
        Guid gameId,
        bool isCurrentUserActive = true,
        GamePlayerView[]? players = null
    ) =>
        new(
            gameId,
            "Arena",
            "ABC123",
            GameStatus.Open,
            2,
            DateTime.UtcNow,
            null,
            "Captain Red",
            CurrentPlayerId,
            players ?? [CurrentPlayer(), OpponentPlayer()],
            isCurrentUserActive
        );
}
