using Orleans;
using Orleans.Runtime;
using Spx.Contracts;
using Spx.Game.Domain;

namespace Spx.Grains;

public sealed class GameSessionGrain(
    [PersistentState("game-session")] IPersistentState<GameSessionGrainState> sessionState)
    : Grain, IGameSessionGrain
{
    private static readonly IGameRoundResolver roundResolver = new GameRoundResolver();

    public async Task InitializeAsync(InitializeGameSessionCommand command)
    {
        GameSessionEngine.Initialize(sessionState.State, command);
        await sessionState.WriteStateAsync();
    }

    public async Task<GameSessionView> SubmitMoveAsync(SubmitGameMoveCommand command)
    {
        var view = GameSessionEngine.SubmitMove(
            sessionState.State,
            this.GetPrimaryKey(),
            command,
            roundResolver,
            DateTime.UtcNow);

        await sessionState.WriteStateAsync();
        return view;
    }

    public Task<GameSessionView?> GetPlayerViewAsync(GetGameSessionViewQuery query)
        => Task.FromResult(GameSessionEngine.GetSessionView(sessionState.State, this.GetPrimaryKey(), query, roundResolver));

    public async Task<GameSessionView> AbandonAsync(AbandonGameSessionCommand command)
    {
        var view = GameSessionEngine.AbandonPlayer(sessionState.State, this.GetPrimaryKey(), command, roundResolver);
        await sessionState.WriteStateAsync();
        return view;
    }
}

[GenerateSerializer]
public sealed class GameSessionGrainState
{
    [Id(0)] public GameSessionParticipantView? FirstPlayer { get; set; }

    [Id(1)] public GameSessionParticipantView? SecondPlayer { get; set; }

    [Id(2)] public bool FirstPlayerActive { get; set; }

    [Id(3)] public bool SecondPlayerActive { get; set; }

    [Id(4)] public int RoundNumber { get; set; } = 1;

    [Id(5)] public GameMove? FirstPlayerMove { get; set; }

    [Id(6)] public GameMove? SecondPlayerMove { get; set; }

    [Id(7)] public ResolvedGameSessionRoundState? LastResolvedRound { get; set; }
}

[GenerateSerializer]
public sealed class ResolvedGameSessionRoundState
{
    [Id(0)] public int RoundNumber { get; set; }

    [Id(1)] public GameMove FirstPlayerMove { get; set; }

    [Id(2)] public GameMove SecondPlayerMove { get; set; }

    [Id(3)] public DateTime ResolvedAtUtc { get; set; }
}

internal static class GameSessionEngine
{
    public static void Initialize(GameSessionGrainState state, InitializeGameSessionCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        if (command.FirstPlayer.PlayerId == command.SecondPlayer.PlayerId)
        {
            throw new InvalidOperationException("A game session requires two distinct players.");
        }

        if (state.FirstPlayer is null && state.SecondPlayer is null)
        {
            state.FirstPlayer = command.FirstPlayer;
            state.SecondPlayer = command.SecondPlayer;
            state.FirstPlayerActive = true;
            state.SecondPlayerActive = true;
            state.RoundNumber = 1;
            state.FirstPlayerMove = null;
            state.SecondPlayerMove = null;
            state.LastResolvedRound = null;
            return;
        }

        if (state.FirstPlayer is not null
            && state.SecondPlayer is not null
            && HasSameRoster(state.FirstPlayer, state.SecondPlayer, command.FirstPlayer, command.SecondPlayer))
        {
            return;
        }

        throw new InvalidOperationException("The game session was already initialized with a different roster.");
    }

    public static GameSessionView SubmitMove(
        GameSessionGrainState state,
        Guid gameId,
        SubmitGameMoveCommand command,
        IGameRoundResolver roundResolver,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(roundResolver);

        EnsureInitialized(state);

        var participant = GetParticipant(state, command.UserId);
        if (!participant.IsActive)
        {
            throw new InvalidOperationException("Inactive players cannot submit moves.");
        }

        if (command.ExpectedRoundNumber != state.RoundNumber)
        {
            throw new InvalidOperationException("The submitted move does not match the current round.");
        }

        if (participant.IsFirstPlayer)
        {
            state.FirstPlayerMove = command.Move;
        }
        else
        {
            state.SecondPlayerMove = command.Move;
        }

        if (state.FirstPlayerActive && state.SecondPlayerActive && state.FirstPlayerMove.HasValue && state.SecondPlayerMove.HasValue)
        {
            state.LastResolvedRound = new ResolvedGameSessionRoundState
            {
                RoundNumber = state.RoundNumber,
                FirstPlayerMove = state.FirstPlayerMove.Value,
                SecondPlayerMove = state.SecondPlayerMove.Value,
                ResolvedAtUtc = nowUtc
            };

            state.RoundNumber++;
            state.FirstPlayerMove = null;
            state.SecondPlayerMove = null;
        }

        return CreatePlayerView(state, gameId, participant.Player.UserId, roundResolver);
    }

    public static GameSessionView? GetSessionView(
        GameSessionGrainState state,
        Guid gameId,
        GetGameSessionViewQuery query,
        IGameRoundResolver roundResolver)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(roundResolver);

        if (state.FirstPlayer is null || state.SecondPlayer is null)
        {
            return null;
        }

        return TryGetParticipant(state, query.UserId) is { } participant
            ? CreatePlayerView(state, gameId, participant.Player.UserId, roundResolver)
            : null;
    }

    public static GameSessionView AbandonPlayer(
        GameSessionGrainState state,
        Guid gameId,
        AbandonGameSessionCommand command,
        IGameRoundResolver roundResolver)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(roundResolver);

        EnsureInitialized(state);

        var participant = GetParticipant(state, command.UserId);
        if (participant.IsFirstPlayer)
        {
            state.FirstPlayerActive = false;
        }
        else
        {
            state.SecondPlayerActive = false;
        }

        state.FirstPlayerMove = null;
        state.SecondPlayerMove = null;

        return CreatePlayerView(state, gameId, participant.Player.UserId, roundResolver);
    }

    private static bool HasSameRoster(
        GameSessionParticipantView existingFirstPlayer,
        GameSessionParticipantView existingSecondPlayer,
        GameSessionParticipantView incomingFirstPlayer,
        GameSessionParticipantView incomingSecondPlayer)
        => (existingFirstPlayer.PlayerId == incomingFirstPlayer.PlayerId && existingSecondPlayer.PlayerId == incomingSecondPlayer.PlayerId)
            || (existingFirstPlayer.PlayerId == incomingSecondPlayer.PlayerId && existingSecondPlayer.PlayerId == incomingFirstPlayer.PlayerId);

    private static void EnsureInitialized(GameSessionGrainState state)
    {
        if (state.FirstPlayer is null || state.SecondPlayer is null)
        {
            throw new InvalidOperationException("The game session has not been initialized.");
        }
    }

    private static ParticipantState GetParticipant(GameSessionGrainState state, string userId)
        => TryGetParticipant(state, userId) ?? throw new InvalidOperationException("The current user is not part of this game session.");

    private static ParticipantState? TryGetParticipant(GameSessionGrainState state, string userId)
    {
        if (state.FirstPlayer is not null && string.Equals(state.FirstPlayer.UserId, userId, StringComparison.Ordinal))
        {
            return new ParticipantState(state.FirstPlayer, state.SecondPlayer!, state.FirstPlayerActive, IsFirstPlayer: true);
        }

        if (state.SecondPlayer is not null && string.Equals(state.SecondPlayer.UserId, userId, StringComparison.Ordinal))
        {
            return new ParticipantState(state.SecondPlayer, state.FirstPlayer!, state.SecondPlayerActive, IsFirstPlayer: false);
        }

        return null;
    }

    private static GameSessionView CreatePlayerView(
        GameSessionGrainState state,
        Guid gameId,
        string userId,
        IGameRoundResolver roundResolver)
    {
        var participant = GetParticipant(state, userId);
        var hasSubmittedMove = participant.IsFirstPlayer ? state.FirstPlayerMove.HasValue : state.SecondPlayerMove.HasValue;
        var opponentHasSubmittedMove = participant.IsFirstPlayer ? state.SecondPlayerMove.HasValue : state.FirstPlayerMove.HasValue;
        var opponentIsActive = participant.IsFirstPlayer ? state.SecondPlayerActive : state.FirstPlayerActive;

        return new GameSessionView(
            gameId,
            state.RoundNumber,
            participant.Player,
            participant.Opponent,
            hasSubmittedMove,
            hasSubmittedMove && opponentIsActive && !opponentHasSubmittedMove,
            CreateRoundResult(state, participant.IsFirstPlayer, roundResolver));
    }

    private static GameSessionRoundResult? CreateRoundResult(
        GameSessionGrainState state,
        bool isFirstPlayer,
        IGameRoundResolver roundResolver)
    {
        if (state.LastResolvedRound is null)
        {
            return null;
        }

        var currentPlayerMove = isFirstPlayer ? state.LastResolvedRound.FirstPlayerMove : state.LastResolvedRound.SecondPlayerMove;
        var opponentMove = isFirstPlayer ? state.LastResolvedRound.SecondPlayerMove : state.LastResolvedRound.FirstPlayerMove;

        return new GameSessionRoundResult(
            state.LastResolvedRound.RoundNumber,
            roundResolver.Resolve(currentPlayerMove, opponentMove),
            state.LastResolvedRound.ResolvedAtUtc);
    }

    private sealed record ParticipantState(GameSessionParticipantView Player, GameSessionParticipantView Opponent, bool IsActive, bool IsFirstPlayer);
}