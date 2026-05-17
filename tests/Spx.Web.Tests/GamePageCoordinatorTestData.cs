using Spx.Contracts;
using Spx.Game.Application;

namespace Spx.Web.Tests;

internal static class GamePageCoordinatorTestData
{
    public static readonly Guid CurrentPlayerId = Guid.Parse("4f4f7dfa-778d-4f65-b8dd-dcde0e6e8f40");
    public static readonly Guid OpponentPlayerId = Guid.Parse("5740ca93-14a6-4c1c-8d08-f5aa7c847f22");

    public static GameLobbyView CreateLobby(Guid gameId, bool isCurrentUserActive = true)
        => new(
            gameId,
            "Arena",
            "ABC123",
            GameStatus.Open,
            2,
            DateTime.UtcNow,
            null,
            "Captain Red",
            [
                new GamePlayerView(CurrentPlayerId, "Captain Red", DateTime.UtcNow, true),
                new GamePlayerView(OpponentPlayerId, "Captain Blue", DateTime.UtcNow, false)
            ],
            isCurrentUserActive);

    public static GameSessionSnapshot CreateSession(Guid gameId, int roundNumber = 1, GameResolvedBatchSnapshot? lastResolvedBatch = null)
        => new(
            gameId,
            roundNumber,
            GamePhase.Play,
            new GamePlayerSnapshot(new GameSessionParticipant(CurrentPlayerId, "user-1"), [], false, 0, 0, false, false, []),
            new GamePlayerSnapshot(new GameSessionParticipant(OpponentPlayerId, "user-2"), [], false, 0, 0, false, true, []),
            [],
            0,
            false,
            true,
            true,
            3,
            lastResolvedBatch,
            null);

    public static GamePageView CreatePage(Guid gameId, GamePresenceView? presence = null, GameSessionSnapshot? session = null)
        => new(CreateLobby(gameId), session ?? CreateSession(gameId), presence ?? GamePresenceView.Empty);

    public static GameResolvedBatchSnapshot CreateResolvedBatch(int roundNumber, DateTime? resolvedAtUtc = null)
        => new(
            roundNumber,
            [
                new GameResolvedPlayerBatchSnapshot(
                    new GameSessionParticipant(CurrentPlayerId, "user-1"),
                    [],
                    false)
            ],
            resolvedAtUtc ?? DateTime.UtcNow);

    public static GameplayEvent CreateGameplayEvent()
        => new(
            GameplayEventKind.Resolved,
            "user-1",
            GameCardDefinition.Extract,
            "user-2",
            GameCardDefinition.Refine,
            null);

    public static IReadOnlyList<GameBatchCardSelection> CreateBatchSelection()
        => [new GameBatchCardSelection(Guid.NewGuid(), null, null, null, null, [])];
}