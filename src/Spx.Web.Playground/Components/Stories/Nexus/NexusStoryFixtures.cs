using Spx.Game.Application;
using Spx.Game.Application.Nexus;
using Spx.Nexus.Domain;
using Spx.Web.Components.Lobby;

namespace Spx.Web.Playground.Components.Stories.Nexus;

internal static class NexusStoryFixtures
{
    public static readonly Guid Player1Id = Spx.Web.Playground.Nexus.PlaygroundNexusUsers.Player1Id;
    public static readonly Guid Player2Id = Spx.Web.Playground.Nexus.PlaygroundNexusUsers.Player2Id;

    public static readonly IReadOnlyDictionary<Guid, string> PlayerNames = Spx.Web
        .Playground
        .Nexus
        .PlaygroundNexusUsers
        .PlayerNames;

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

    public static GameLobbyView CreateWaitingLobby(Guid gameId) =>
        new(
            gameId,
            "Awaiting Challenger",
            "WAIT01",
            GameStatus.Open,
            2,
            DateTime.UtcNow.AddMinutes(-45),
            null,
            PlayerNames[Player1Id],
            Player1Id,
            [
                new GamePlayerView(
                    Player1Id,
                    PlayerNames[Player1Id],
                    DateTime.UtcNow.AddMinutes(-45)
                ),
            ],
            true
        );

    public static GameLobbyView CreateArchivedLobby(Guid gameId) =>
        new(
            gameId,
            "Archived Match",
            "ARCH01",
            GameStatus.Ended,
            2,
            DateTime.UtcNow.AddHours(-5),
            DateTime.UtcNow.AddHours(-2),
            PlayerNames[Player1Id],
            Player1Id,
            [
                new GamePlayerView(Player1Id, PlayerNames[Player1Id], DateTime.UtcNow.AddHours(-5)),
                new GamePlayerView(
                    Player2Id,
                    PlayerNames[Player2Id],
                    DateTime.UtcNow.AddHours(-4).AddMinutes(-40)
                ),
            ],
            false
        );

    public static UserGamesView CreateUserGamesView() =>
        new(
            [
                new GameSummaryView(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    "Saturday skirmish",
                    "SAT201",
                    GameStatus.Open,
                    2,
                    2,
                    DateTime.UtcNow.AddHours(-4),
                    null,
                    "Captain Red"
                ),
                new GameSummaryView(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    "Slow-burn lobby",
                    "WAIT22",
                    GameStatus.Open,
                    1,
                    2,
                    DateTime.UtcNow.AddMinutes(-90),
                    null,
                    "Captain Red"
                ),
            ],
            [
                new GameSummaryView(
                    Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    "Finished rivalry",
                    "DONE33",
                    GameStatus.Ended,
                    0,
                    2,
                    DateTime.UtcNow.AddDays(-2),
                    DateTime.UtcNow.AddDays(-1),
                    "Captain Red"
                ),
            ]
        );

    public static UserGamesView CreateEmptyUserGamesView() => new([], []);

    public static GamePresenceView CreatePresence(params Guid[] onlinePlayerIds) =>
        new(onlinePlayerIds);

    public static IReadOnlyList<TimelineEntryState> CreateTimelineEntries()
    {
        var now = DateTime.UtcNow;

        return
        [
            new TimelineEntryState
            {
                Key = Guid.Parse("44444444-4444-4444-4444-444444444441"),
                Message = new GameTimelineEntryView(
                    Guid.Parse("44444444-4444-4444-4444-444444444441"),
                    GameMessageKind.GameCreated,
                    GameMessageSenderKind.Game,
                    Player1Id,
                    PlayerNames[Player1Id],
                    null,
                    string.Empty,
                    string.Empty,
                    now.AddMinutes(-35),
                    null,
                    null,
                    false,
                    false,
                    false,
                    false
                ),
            },
            new TimelineEntryState
            {
                Key = Guid.Parse("44444444-4444-4444-4444-444444444442"),
                Message = new GameTimelineEntryView(
                    Guid.Parse("44444444-4444-4444-4444-444444444442"),
                    GameMessageKind.PlayerJoined,
                    GameMessageSenderKind.Game,
                    Player2Id,
                    PlayerNames[Player2Id],
                    null,
                    string.Empty,
                    string.Empty,
                    now.AddMinutes(-32),
                    null,
                    null,
                    false,
                    false,
                    false,
                    false
                ),
            },
            new TimelineEntryState
            {
                Key = Guid.Parse("44444444-4444-4444-4444-444444444443"),
                Message = new GameTimelineEntryView(
                    Guid.Parse("44444444-4444-4444-4444-444444444443"),
                    GameMessageKind.PlayerPublic,
                    GameMessageSenderKind.Player,
                    Player1Id,
                    PlayerNames[Player1Id],
                    null,
                    string.Empty,
                    "Opening move is set. I am pushing through the north lane.",
                    now.AddMinutes(-18),
                    now.AddMinutes(-16),
                    null,
                    true,
                    false,
                    true,
                    true
                ),
            },
            new TimelineEntryState
            {
                Key = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Message = new GameTimelineEntryView(
                    Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    GameMessageKind.PlayerPrivate,
                    GameMessageSenderKind.Player,
                    Player2Id,
                    PlayerNames[Player2Id],
                    Player1Id,
                    PlayerNames[Player1Id],
                    "Private note: your southern flank is open if you want a feint.",
                    now.AddMinutes(-12),
                    null,
                    null,
                    false,
                    true,
                    false,
                    false
                ),
            },
            new TimelineEntryState
            {
                Key = Guid.Parse("44444444-4444-4444-4444-444444444445"),
                Message = new GameTimelineEntryView(
                    Guid.Parse("44444444-4444-4444-4444-444444444445"),
                    GameMessageKind.GameplayEvent,
                    GameMessageSenderKind.Game,
                    null,
                    "System",
                    null,
                    string.Empty,
                    "Carrier group moved from H-3 to G-2. Income recalculated for round 4.",
                    now.AddMinutes(-8),
                    null,
                    null,
                    false,
                    false,
                    false,
                    false
                ),
            },
            new TimelineEntryState
            {
                Key = Guid.Parse("44444444-4444-4444-4444-444444444446"),
                Pending = new PendingMessageState(
                    "Queued private reply while the connection recovers.",
                    Player2Id,
                    PlayerNames[Player2Id],
                    now.AddMinutes(-2),
                    true,
                    false
                ),
            },
            new TimelineEntryState
            {
                Key = Guid.Parse("44444444-4444-4444-4444-444444444447"),
                Pending = new PendingMessageState(
                    "Retry this failed public message.",
                    null,
                    string.Empty,
                    now.AddMinutes(-1),
                    false,
                    true
                ),
            },
        ];
    }

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
                ImmutableDictionary<NexusUnitType, int>.Empty.Add(NexusUnitType.Carrier, 1),
                IsRetreat: false
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
        var state = new NexusState();
        NexusEngine.Initialize(
            state,
            new InitializeNexusGameCommand(
                ImmutableArray.Create(
                    new NexusSessionPlayer(Player1Id),
                    new NexusSessionPlayer(Player2Id)
                )
            ),
            new Random(42)
        );

        var rng = new Random(0);
        for (var i = 0; i < rounds; i++)
        {
            NexusEngine.SubmitOrders(
                state,
                new NexusTurnOrdersCommand(Player1Id, state.RoundNumber, [], [], false),
                rng
            );
            NexusEngine.SubmitOrders(
                state,
                new NexusTurnOrdersCommand(Player2Id, state.RoundNumber, [], [], false),
                rng
            );
        }

        return new GameplayScenario(
            label,
            description,
            NexusEngine.BuildView(state, Guid.NewGuid(), Player1Id)
        );
    }

    private static GameplayScenario BuildWaitingScenario()
    {
        var state = new NexusState();
        NexusEngine.Initialize(
            state,
            new InitializeNexusGameCommand(
                ImmutableArray.Create(
                    new NexusSessionPlayer(Player1Id),
                    new NexusSessionPlayer(Player2Id)
                )
            ),
            new Random(42)
        );

        var rng = new Random(0);
        NexusEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(Player1Id, 1, [], [], false),
            rng
        );
        NexusEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(Player2Id, 1, [], [], false),
            rng
        );
        NexusEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(Player2Id, 2, [], [], false),
            rng
        );

        return new GameplayScenario(
            "Waiting",
            "P2 has already submitted orders; P1 still needs to commit.",
            NexusEngine.BuildView(state, Guid.NewGuid(), Player1Id)
        );
    }

    private static GameplayScenario BuildEndedScenario()
    {
        var state = new NexusState();
        NexusEngine.Initialize(
            state,
            new InitializeNexusGameCommand(
                ImmutableArray.Create(
                    new NexusSessionPlayer(Player1Id),
                    new NexusSessionPlayer(Player2Id)
                )
            ),
            new Random(42)
        );

        NexusEngine.Abandon(state, Player2Id);

        return new GameplayScenario(
            "Ended",
            "P2 abandoned; P1 wins.",
            NexusEngine.BuildView(state, Guid.NewGuid(), Player1Id)
        );
    }
}
