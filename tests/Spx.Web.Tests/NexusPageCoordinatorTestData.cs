using System.Collections.Immutable;
using Spx.Contracts;
using Spx.Game.Application;
using Spx.Nexus.Domain;

namespace Spx.Web.Tests;

internal static class GamePageCoordinatorTestData
{
    public static readonly Guid CurrentPlayerId = Guid.Parse(
        "4f4f7dfa-778d-4f65-b8dd-dcde0e6e8f40"
    );
    public static readonly Guid OpponentPlayerId = Guid.Parse(
        "5740ca93-14a6-4c1c-8d08-f5aa7c847f22"
    );
    public static readonly HexCoord CurrentPlayerHomeCoord = NexusMapTopology.Player1HomeCoord;
    public static readonly HexCoord MoveTargetCoord = new(1, -2);
    public static readonly HexCoord AlternateFocusCoord = new(2, -1);

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
                false,
                0,
                0
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
                false,
                0,
                0
            ),
            [],
            null
        );

    public static NexusGameView CreateGameplayPanelSession(
        Guid gameId,
        int roundNumber = 1,
        ImmutableArray<NexusResolveEvent>? lastResolveEvents = null,
        int currentPlayerEnergy = 20
    )
    {
        var baseSession = CreateSession(gameId, roundNumber);

        return baseSession with
        {
            Systems =
            [
                CreateSystem(
                    CurrentPlayerHomeCoord,
                    homePlayerId: CurrentPlayerId,
                    controlOwner: CurrentPlayerId,
                    unitStacks: ImmutableDictionary<
                        Guid,
                        ImmutableArray<NexusUnitStackGroup>
                    >.Empty.Add(
                        CurrentPlayerId,
                        FullHullStacks(
                            (NexusUnitType.Carrier, 1),
                            (NexusUnitType.Fighter, 1),
                            (NexusUnitType.Infantry, 2)
                        )
                    )
                ),
                CreateSystem(MoveTargetCoord, incomeValue: 1),
                CreateSystem(AlternateFocusCoord, incomeValue: 2),
                CreateSystem(
                    NexusMapTopology.Player2HomeCoord,
                    homePlayerId: OpponentPlayerId,
                    controlOwner: OpponentPlayerId,
                    unitStacks: ImmutableDictionary<
                        Guid,
                        ImmutableArray<NexusUnitStackGroup>
                    >.Empty.Add(OpponentPlayerId, FullHullStacks((NexusUnitType.Fighter, 1)))
                ),
            ],
            CurrentPlayer = baseSession.CurrentPlayer with { Energy = currentPlayerEnergy },
            LastResolveEvents = lastResolveEvents ?? [],
        };
    }

    public static ImmutableArray<NexusResolveEvent> CreateGameplayPanelResolveEvents() =>
        [
            new NexusUnitsMovedEvent(
                CurrentPlayerId,
                CurrentPlayerHomeCoord,
                MoveTargetCoord,
                FullHullStacks((NexusUnitType.Fighter, 1)),
                IsRetreat: false
            ),
            new NexusPlanetaryControlEvent(AlternateFocusCoord, CurrentPlayerId),
        ];

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

    private static NexusSystemView CreateSystem(
        HexCoord coord,
        bool isNexus = false,
        int incomeValue = 0,
        Guid? homePlayerId = null,
        Guid? controlOwner = null,
        ImmutableDictionary<Guid, ImmutableArray<NexusUnitStackGroup>>? unitStacks = null
    ) =>
        new(
            coord,
            isNexus,
            incomeValue,
            homePlayerId,
            controlOwner,
            unitStacks ?? ImmutableDictionary<Guid, ImmutableArray<NexusUnitStackGroup>>.Empty
        );

    private static ImmutableArray<NexusUnitStackGroup> FullHullStacks(
        params (NexusUnitType Type, int Count)[] units
    ) =>
        units
            .Select(unit => new NexusUnitStackGroup(unit.Type, unit.Type.Hull(), unit.Count))
            .ToImmutableArray();
}
