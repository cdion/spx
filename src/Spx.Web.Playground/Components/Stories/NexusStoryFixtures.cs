using Spx.Game.Application;
using Spx.Game.Domain;

namespace Spx.Web.Playground.Components.Stories;

internal static class NexusStoryFixtures
{
    public static readonly Guid Player1Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid Player2Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    public static readonly IReadOnlyDictionary<Guid, string> PlayerNames = new Dictionary<
        Guid,
        string
    >
    {
        [Player1Id] = "Player One",
        [Player2Id] = "Player Two",
    };

    public static GameLobbyView CreateLobby(Guid gameId) =>
        new(
            gameId,
            "Playground Game",
            "PLAY01",
            GameStatus.Open,
            2,
            DateTime.UtcNow.AddHours(-1),
            null,
            PlayerNames[Player1Id],
            Player1Id,
            [
                new GamePlayerView(Player1Id, PlayerNames[Player1Id], DateTime.UtcNow.AddHours(-1)),
                new GamePlayerView(
                    Player2Id,
                    PlayerNames[Player2Id],
                    DateTime.UtcNow.AddMinutes(-58)
                ),
            ],
            true
        );

    public static IReadOnlyList<GameplayScenario> CreateGameplayScenarios() =>
        [
            BuildScenario(
                "Round 1",
                "Fresh game: 0 energy, can't build or start a gate.",
                rounds: 0
            ),
            BuildScenario("Round 2", "After one round of income: 3 energy each.", rounds: 1),
            BuildScenario(
                "Round 5",
                "Five rounds in: more energy, multiple controlled systems.",
                rounds: 4
            ),
            BuildWaitingScenario(),
            BuildEndedScenario(),
        ];

    public static IReadOnlyList<NexusResolveEvent> CreateSampleResolveEvents() =>
        [
            new NexusUnitsMovedEvent(
                Player1Id,
                NexusMap.Player1HomeCoord,
                new HexCoord(1, -2),
                ImmutableDictionary<NexusUnitType, int>.Empty.Add(NexusUnitType.Carrier, 1)
            ),
            new NexusCombatBeganEvent(new HexCoord(1, -2), Player1Id, Player2Id),
            new NexusIncomeEvent(
                Player1Id,
                11,
                ImmutableArray.Create(
                    NexusMap.Player1HomeCoord,
                    new HexCoord(1, -2),
                    new HexCoord(0, -1)
                )
            ),
        ];

    public static IReadOnlyList<SelectedHexScenario> CreateSelectedHexScenarios() =>
        [
            new(
                "Home",
                new NexusSystemView(
                    NexusMap.Player1HomeCoord,
                    false,
                    3,
                    Player1Id,
                    Player1Id,
                    ImmutableDictionary<Guid, ImmutableDictionary<NexusUnitType, int>>.Empty.Add(
                        Player1Id,
                        ImmutableDictionary<NexusUnitType, int>
                            .Empty.Add(NexusUnitType.Carrier, 1)
                            .Add(NexusUnitType.Fighter, 2)
                            .Add(NexusUnitType.Infantry, 4)
                    )
                ),
                ImmutableDictionary<NexusUnitType, int>
                    .Empty.Add(NexusUnitType.Carrier, 1)
                    .Add(NexusUnitType.Fighter, 2)
                    .Add(NexusUnitType.Infantry, 4),
                NexusGateProgress.None
            ),
            new(
                "Nexus",
                new NexusSystemView(
                    NexusMap.NexusCoord,
                    true,
                    0,
                    null,
                    Player1Id,
                    ImmutableDictionary<Guid, ImmutableDictionary<NexusUnitType, int>>
                        .Empty.Add(
                            Player1Id,
                            ImmutableDictionary<NexusUnitType, int>
                                .Empty.Add(NexusUnitType.Carrier, 1)
                                .Add(NexusUnitType.Infantry, 2)
                        )
                        .Add(
                            Player2Id,
                            ImmutableDictionary<NexusUnitType, int>
                                .Empty.Add(NexusUnitType.Cruiser, 1)
                                .Add(NexusUnitType.Infantry, 1)
                        )
                ),
                ImmutableDictionary<NexusUnitType, int>
                    .Empty.Add(NexusUnitType.Carrier, 1)
                    .Add(NexusUnitType.Infantry, 2),
                NexusGateProgress.Started
            ),
            new(
                "Income",
                new NexusSystemView(
                    new HexCoord(1, -1),
                    false,
                    4,
                    null,
                    null,
                    ImmutableDictionary<Guid, ImmutableDictionary<NexusUnitType, int>>.Empty.Add(
                        Player1Id,
                        ImmutableDictionary<NexusUnitType, int>
                            .Empty.Add(NexusUnitType.Destroyer, 1)
                            .Add(NexusUnitType.Fighter, 1)
                    )
                ),
                ImmutableDictionary<NexusUnitType, int>
                    .Empty.Add(NexusUnitType.Destroyer, 1)
                    .Add(NexusUnitType.Fighter, 1),
                NexusGateProgress.None
            ),
        ];

    public sealed record GameplayScenario(string Label, string Description, NexusGameView View);

    public sealed record SelectedHexScenario(
        string Label,
        NexusSystemView System,
        ImmutableDictionary<NexusUnitType, int> AvailableUnits,
        NexusGateProgress GateProgress
    );

    private static GameplayScenario BuildScenario(string label, string description, int rounds)
    {
        var state = new NexusGameState();
        NexusGameEngine.Initialize(
            state,
            new InitializeNexusGameCommand(
                ImmutableArray.Create(
                    new GameSessionParticipant(Player1Id),
                    new GameSessionParticipant(Player2Id)
                )
            ),
            new Random(42)
        );

        var rng = new Random(0);
        for (var i = 0; i < rounds; i++)
        {
            NexusGameEngine.SubmitOrders(
                state,
                new NexusTurnOrdersCommand(Player1Id, state.RoundNumber, [], [], false),
                rng
            );
            NexusGameEngine.SubmitOrders(
                state,
                new NexusTurnOrdersCommand(Player2Id, state.RoundNumber, [], [], false),
                rng
            );
        }

        return new GameplayScenario(
            label,
            description,
            NexusGameEngine.BuildView(state, Guid.NewGuid(), Player1Id)
        );
    }

    private static GameplayScenario BuildWaitingScenario()
    {
        var state = new NexusGameState();
        NexusGameEngine.Initialize(
            state,
            new InitializeNexusGameCommand(
                ImmutableArray.Create(
                    new GameSessionParticipant(Player1Id),
                    new GameSessionParticipant(Player2Id)
                )
            ),
            new Random(42)
        );

        var rng = new Random(0);
        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(Player1Id, 1, [], [], false),
            rng
        );
        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(Player2Id, 1, [], [], false),
            rng
        );
        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(Player2Id, 2, [], [], false),
            rng
        );

        return new GameplayScenario(
            "Waiting",
            "P2 has already submitted orders; P1 still needs to commit.",
            NexusGameEngine.BuildView(state, Guid.NewGuid(), Player1Id)
        );
    }

    private static GameplayScenario BuildEndedScenario()
    {
        var state = new NexusGameState();
        NexusGameEngine.Initialize(
            state,
            new InitializeNexusGameCommand(
                ImmutableArray.Create(
                    new GameSessionParticipant(Player1Id),
                    new GameSessionParticipant(Player2Id)
                )
            ),
            new Random(42)
        );

        NexusGameEngine.Abandon(state, Player2Id);

        return new GameplayScenario(
            "Ended",
            "P2 abandoned; P1 wins.",
            NexusGameEngine.BuildView(state, Guid.NewGuid(), Player1Id)
        );
    }
}
