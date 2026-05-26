using Microsoft.Extensions.Logging;
using Orleans;
using Spx.Contracts;
using Spx.Game.Domain;

namespace Spx.Grains;

public sealed partial class NexusGameSessionGrain(
    [PersistentState("game")] IPersistentState<NexusGameSessionGrainState> state,
    ILogger<NexusGameSessionGrain> logger
) : Grain, IGameSessionGrain
{
    private Guid _gameId;

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _gameId = this.GetPrimaryKey();
        return base.OnActivateAsync(cancellationToken);
    }

    public async Task InitializeAsync(InitializeNexusGameCommand command)
    {
        NexusGameEngine.Initialize(state.State.Game, command, new Random());
        await state.WriteStateAsync();
        await GrainFactory.GetGrain<IGameInvalidationGrain>(_gameId).PublishSessionInvalidated();
        LogInitialized(logger, _gameId);
    }

    public async Task<NexusTurnOrdersResult> SubmitOrdersAsync(NexusTurnOrdersCommand command)
    {
        var result = NexusGameEngine.SubmitOrders(state.State.Game, command, new Random());
        if (result is NexusTurnOrdersAccepted)
        {
            await state.WriteStateAsync();
            await GrainFactory
                .GetGrain<IGameInvalidationGrain>(_gameId)
                .PublishSessionInvalidated();
        }

        return result;
    }

    public Task<NexusGameView?> GetViewAsync(Guid playerId)
    {
        if (state.State.Game.Players.Count == 0)
            return Task.FromResult<NexusGameView?>(null);

        var view = NexusGameEngine.BuildView(state.State.Game, _gameId, playerId);
        return Task.FromResult<NexusGameView?>(view);
    }

    public async Task AbandonAsync(Guid playerId)
    {
        NexusGameEngine.Abandon(state.State.Game, playerId);
        await state.WriteStateAsync();
        await GrainFactory.GetGrain<IGameInvalidationGrain>(_gameId).PublishSessionInvalidated();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Game {GameId} initialized.")]
    private static partial void LogInitialized(ILogger logger, Guid gameId);
}
