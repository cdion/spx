using Microsoft.Extensions.Logging;
using Orleans;
using Spx.Contracts;
using Spx.Nexus.Domain;

namespace Spx.Grains;

public sealed partial class NexusSessionGrain(
    [PersistentState("game")] IPersistentState<NexusSessionGrainState> state,
    ILogger<NexusSessionGrain> logger
) : Grain, INexusSessionGrain
{
    private Guid _gameId;

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _gameId = this.GetPrimaryKey();
        return base.OnActivateAsync(cancellationToken);
    }

    public async Task InitializeAsync(InitializeNexusGameCommand command)
    {
        NexusEngine.Initialize(state.State.Game, command, new Random());
        await state.WriteStateAsync();
        await GrainFactory.GetGrain<ILobbyInvalidationGrain>(_gameId).PublishSessionInvalidated();
        LogInitialized(logger, _gameId);
    }

    public async Task<NexusTurnOrdersResult> SubmitOrdersAsync(NexusTurnOrdersCommand command)
    {
        var result = NexusEngine.SubmitOrders(state.State.Game, command, new Random());
        if (result is NexusTurnOrdersAccepted)
        {
            await state.WriteStateAsync();
            await GrainFactory
                .GetGrain<ILobbyInvalidationGrain>(_gameId)
                .PublishSessionInvalidated();
        }

        return result;
    }

    public Task<NexusGameView?> GetViewAsync(Guid playerId)
    {
        if (state.State.Game.Players.Count == 0)
            return Task.FromResult<NexusGameView?>(null);

        var view = NexusEngine.BuildView(state.State.Game, _gameId, playerId);
        return Task.FromResult<NexusGameView?>(view);
    }

    public async Task AbandonAsync(Guid playerId)
    {
        NexusEngine.Abandon(state.State.Game, playerId);
        await state.WriteStateAsync();
        await GrainFactory.GetGrain<ILobbyInvalidationGrain>(_gameId).PublishSessionInvalidated();
    }

    public async Task<NexusDesignCommandResult> CreateDesignAsync(NexusCreateDesignCommand command)
    {
        var result = NexusEngine.CreateDesign(state.State.Game, command);
        if (result is NexusDesignCreated)
        {
            await state.WriteStateAsync();
            await GrainFactory
                .GetGrain<ILobbyInvalidationGrain>(_gameId)
                .PublishSessionInvalidated();
        }
        return result;
    }

    public async Task<NexusDesignCommandResult> DeleteDesignAsync(NexusDeleteDesignCommand command)
    {
        var result = NexusEngine.DeleteDesign(state.State.Game, command);
        if (result is NexusDesignDeleted)
        {
            await state.WriteStateAsync();
            await GrainFactory
                .GetGrain<ILobbyInvalidationGrain>(_gameId)
                .PublishSessionInvalidated();
        }
        return result;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Game {GameId} initialized.")]
    private static partial void LogInitialized(ILogger logger, Guid gameId);
}
