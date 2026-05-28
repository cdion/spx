using System.Collections.Immutable;
using Spx.Game.Application;
using Spx.Game.Application.Nexus;
using Spx.Nexus.Domain;
using Spx.Nexus.Mapping;

namespace Spx.Web.Playground.Nexus;

internal sealed class PlaygroundNexusHarness
    : INexusSessionService,
        INexusSessionRosterProvider,
        IGamePresenceService,
        IGamePersistence,
        IGameMessagePersistence,
        ILobbyInvalidationPublisher,
        INexusSessionInvalidationPublisher,
        IGameMessageInvalidationPublisher
{
    private readonly Dictionary<Guid, SessionEntry> sessions = [];

    public Task<bool> EnsureSessionAsync(
        Guid gameId,
        IReadOnlyList<Guid> playerIds,
        CancellationToken cancellationToken = default
    )
    {
        if (playerIds.Count != 2)
        {
            return Task.FromResult(false);
        }

        if (sessions.ContainsKey(gameId))
        {
            return Task.FromResult(true);
        }

        var state = new NexusState();
        var random = new Random(42);
        var players = playerIds.ToImmutableArray();

        NexusEngine.Initialize(
            state,
            new InitializeNexusGameCommand([
                .. players.Select(playerId => new NexusSessionPlayer(playerId)),
            ]),
            random
        );

        sessions[gameId] = new SessionEntry(gameId, state, random, DateTime.UtcNow, players);
        return Task.FromResult(true);
    }

    public Task<GameSessionOutcome> GetSessionAsync(
        Guid gameId,
        Guid playerId,
        CancellationToken cancellationToken = default
    )
    {
        if (!sessions.TryGetValue(gameId, out var session))
        {
            return Task.FromResult<GameSessionOutcome>(new GameSessionUnavailable());
        }

        var view = NexusEngine.BuildView(session.State, gameId, playerId);
        return Task.FromResult<GameSessionOutcome>(
            new GameSessionFound(NexusSeamMapper.ToApplication(view))
        );
    }

    public async Task<GameSessionCommandOutcome> SubmitOrdersAsync(
        Guid gameId,
        NexusSubmitTurnCommand command,
        CancellationToken cancellationToken = default
    )
    {
        if (!sessions.TryGetValue(gameId, out var session))
        {
            return new GameSessionCommandFailed("Game session unavailable.");
        }

        var result = NexusEngine.SubmitOrders(
            session.State,
            NexusSeamMapper.ToDomain(command),
            session.Random
        );
        if (result is NexusTurnOrdersRejected rejected)
        {
            return new GameSessionCommandFailed(rejected.ErrorMessage);
        }

        var sessionOutcome = await GetSessionAsync(gameId, command.PlayerId, cancellationToken);
        return sessionOutcome is GameSessionFound found
            ? new GameSessionCommandSucceeded(found.Session)
            : new GameSessionCommandFailed("Game session unavailable.");
    }

    public Task AbandonAsync(
        Guid gameId,
        Guid playerId,
        CancellationToken cancellationToken = default
    )
    {
        if (sessions.TryGetValue(gameId, out var session))
        {
            NexusEngine.Abandon(session.State, playerId);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Guid>?> GetActiveSessionPlayersAsync(
        Guid gameId,
        CancellationToken cancellationToken
    ) => Task.FromResult<IReadOnlyList<Guid>?>(PlaygroundNexusUsers.PlayerIds);

    public Task<GamePresenceView> GetPresenceAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(new GamePresenceView([.. PlaygroundNexusUsers.PlayerIds]));

    public Task<GameLobbyView?> GetLobbyAsync(
        Guid gameId,
        string userId,
        CancellationToken cancellationToken
    )
    {
        if (!sessions.TryGetValue(gameId, out var session))
        {
            return Task.FromResult<GameLobbyView?>(null);
        }

        if (
            !PlaygroundNexusUsers.TryGetViewer(
                userId,
                out var currentPlayerId,
                out var currentPlayerName
            )
        )
        {
            return Task.FromResult<GameLobbyView?>(null);
        }

        return Task.FromResult<GameLobbyView?>(
            new GameLobbyView(
                gameId,
                "Playground Game",
                "PLAY01",
                GameStatus.Open,
                2,
                session.CreatedAtUtc,
                null,
                currentPlayerName,
                currentPlayerId,
                CreatePlayers(session.CreatedAtUtc),
                true
            )
        );
    }

    public Task<IReadOnlyList<GamePlayerView>> GetActivePlayersAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        var createdAtUtc = sessions.TryGetValue(gameId, out var session)
            ? session.CreatedAtUtc
            : DateTime.UtcNow;
        return Task.FromResult(CreatePlayers(createdAtUtc));
    }

    public Task<UserGamesView> GetUserGamesAsync(
        string userId,
        CancellationToken cancellationToken
    ) => Task.FromResult(new UserGamesView([], []));

    public Task<Guid?> TryCreateGameAsync(
        CreateGamePersistenceRequest request,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException();

    public Task<JoinGamePersistenceResult> JoinGameAsync(
        JoinGamePersistenceRequest request,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException();

    public Task<LeaveGamePersistenceResult> LeaveGameAsync(
        Guid gameId,
        string userId,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException();

    public Task<GameTimelinePageView?> GetMessagesAsync(
        Guid gameId,
        Guid playerId,
        Guid? beforeMessageId,
        int take,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException();

    public Task<IReadOnlyList<GameTimelineEntryView>?> GetMessageUpdatesAsync(
        Guid gameId,
        Guid playerId,
        Guid? afterMessageId,
        int take,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException();

    public Task<GameMessageCommandOutcome> SendPublicMessageAsync(
        Guid gameId,
        Guid playerId,
        string body,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException();

    public Task<GameMessageCommandOutcome> SendPrivateMessageAsync(
        Guid gameId,
        Guid playerId,
        Guid recipientPlayerId,
        string body,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException();

    public Task<GameMessageCommandOutcome> EditMessageAsync(
        Guid gameId,
        Guid playerId,
        Guid messageId,
        string body,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException();

    public Task<GameMessageCommandOutcome> DeleteMessageAsync(
        Guid gameId,
        Guid playerId,
        Guid messageId,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException();

    public Task WriteGameplayEventsAsync(
        Guid gameId,
        IReadOnlyList<string> bodies,
        CancellationToken cancellationToken = default
    )
    {
        if (sessions.TryGetValue(gameId, out var session))
        {
            session.GameplayEvents.AddRange(bodies);
        }

        return Task.CompletedTask;
    }

    public Task PublishLobbyInvalidatedAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    ) => Task.CompletedTask;

    public Task PublishSessionInvalidatedAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    ) => Task.CompletedTask;

    public Task PublishMessagesInvalidatedAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    ) => Task.CompletedTask;

    private static IReadOnlyList<GamePlayerView> CreatePlayers(DateTime createdAtUtc) =>
        [
            new GamePlayerView(
                PlaygroundNexusUsers.Player1Id,
                PlaygroundNexusUsers.PlayerNames[PlaygroundNexusUsers.Player1Id],
                createdAtUtc
            ),
            new GamePlayerView(
                PlaygroundNexusUsers.Player2Id,
                PlaygroundNexusUsers.PlayerNames[PlaygroundNexusUsers.Player2Id],
                createdAtUtc.AddMinutes(2)
            ),
        ];

    private sealed class SessionEntry(
        Guid gameId,
        NexusState state,
        Random random,
        DateTime createdAtUtc,
        ImmutableArray<Guid> playerIds
    )
    {
        public Guid GameId { get; } = gameId;

        public NexusState State { get; } = state;

        public Random Random { get; } = random;

        public DateTime CreatedAtUtc { get; } = createdAtUtc;

        public ImmutableArray<Guid> PlayerIds { get; } = playerIds;

        public List<string> GameplayEvents { get; } = [];
    }
}
