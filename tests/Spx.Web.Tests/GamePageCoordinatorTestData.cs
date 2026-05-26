using System.Collections.Immutable;
using Spx.Contracts;
using Spx.Game.Application;

namespace Spx.Web.Tests;

internal static class GamePageCoordinatorTestData
{
    public static readonly Guid CurrentPlayerId = Guid.Parse(
        "4f4f7dfa-778d-4f65-b8dd-dcde0e6e8f40"
    );
    public static readonly Guid OpponentPlayerId = Guid.Parse(
        "5740ca93-14a6-4c1c-8d08-f5aa7c847f22"
    );

    public static GameLobbyView CreateLobby(Guid gameId, bool isCurrentUserActive = true) =>
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
            [
                new GamePlayerView(CurrentPlayerId, "Captain Red", DateTime.UtcNow),
                new GamePlayerView(OpponentPlayerId, "Captain Blue", DateTime.UtcNow),
            ],
            isCurrentUserActive
        );

    public static NexusGameView CreateSession(Guid gameId, int roundNumber = 1) =>
        new(
            gameId,
            roundNumber,
            [],
            new NexusPlayerView(
                CurrentPlayerId,
                NexusFactionColor.Red,
                0,
                NexusGateProgress.None,
                false,
                true,
                [],
                null,
                false
            ),
            new NexusPlayerView(
                OpponentPlayerId,
                NexusFactionColor.Blue,
                0,
                NexusGateProgress.None,
                false,
                true,
                null,
                null,
                false
            ),
            [],
            null
        );

    public static GamePageView CreatePage(
        Guid gameId,
        GamePresenceView? presence = null,
        NexusGameView? session = null
    ) =>
        new(
            CreateLobby(gameId),
            session ?? CreateSession(gameId),
            presence ?? GamePresenceView.Empty
        );
}
