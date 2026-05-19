using Orleans;
using Orleans.Runtime;
using Spx.Contracts;
using Spx.Game.Domain;

namespace Spx.Grains;

public sealed class GameSessionGrain(
    [PersistentState("game-session")] IPersistentState<GameSessionGrainState> sessionState
) : Grain, IGameSessionGrain
{
    public async Task InitializeAsync(InitializeGameSessionCommand command)
    {
        var workingState = GameSessionGrainStateMapper.ToDomainState(sessionState.State);
        GameSessionEngine.Initialize(workingState, command);
        sessionState.State = GameSessionGrainStateMapper.FromDomainState(
            workingState,
            sessionState.State.PendingGameplayEventBatches
        );
        await sessionState.WriteStateAsync();
    }

    public async Task<GameSessionGrainCommandResult> SubmitAcquireAsync(
        SubmitAcquireCommand command
    )
    {
        var workingState = GameSessionGrainStateMapper.ToDomainState(sessionState.State);
        var result = GameSessionEngine.SubmitAcquire(workingState, this.GetPrimaryKey(), command);

        if (result is GameSessionCommandSucceededResult)
        {
            sessionState.State = GameSessionGrainStateMapper.FromDomainState(
                workingState,
                sessionState.State.PendingGameplayEventBatches
            );
            await sessionState.WriteStateAsync();
        }

        return MapCommandResult(result);
    }

    public async Task<GameSessionGrainCommandResult> SubmitPlayBatchAsync(
        SubmitPlayBatchCommand command
    )
    {
        var workingState = GameSessionGrainStateMapper.ToDomainState(sessionState.State);
        var result = GameSessionEngine.SubmitPlayBatch(
            workingState,
            this.GetPrimaryKey(),
            command,
            DateTime.UtcNow
        );

        Guid? pendingGameplayEventBatchId = null;

        if (result is GameSessionCommandSucceededResult succeeded)
        {
            var mappedState = GameSessionGrainStateMapper.FromDomainState(
                workingState,
                sessionState.State.PendingGameplayEventBatches
            );
            if (succeeded.GameplayEvents.Count > 0)
            {
                pendingGameplayEventBatchId = Guid.NewGuid();
                mappedState.PendingGameplayEventBatches.Add(
                    new PendingGameplayEventBatchGrainState
                    {
                        BatchId = pendingGameplayEventBatchId.Value,
                        GameId = this.GetPrimaryKey(),
                        LastResolvedBatch = succeeded.Session.LastResolvedBatch,
                        Completion = succeeded.Session.Completion,
                        GameplayEvents = [.. succeeded.GameplayEvents],
                    }
                );
            }

            sessionState.State = mappedState;
            await sessionState.WriteStateAsync();
        }

        return MapCommandResult(result, pendingGameplayEventBatchId);
    }

    public Task<GameSessionView?> GetPlayerViewAsync(GetGameSessionQuery query) =>
        Task.FromResult(
            GameSessionEngine.GetSessionView(
                GameSessionGrainStateMapper.ToDomainState(sessionState.State),
                this.GetPrimaryKey(),
                query
            )
        );

    public Task<
        IReadOnlyList<PendingGameplayEventBatchGrainView>
    > GetPendingGameplayEventBatchesAsync() =>
        Task.FromResult<IReadOnlyList<PendingGameplayEventBatchGrainView>>(
            sessionState
                .State.PendingGameplayEventBatches.Select(
                    batch => new PendingGameplayEventBatchGrainView(
                        batch.BatchId,
                        batch.GameId,
                        batch.LastResolvedBatch,
                        batch.Completion,
                        batch.GameplayEvents.ToArray()
                    )
                )
                .ToList()
        );

    public Task AcknowledgeGameplayEventBatchesAsync(
        AcknowledgeGameplayEventBatchesGrainCommand command
    )
    {
        var batchIds = command.BatchIds.ToHashSet();
        sessionState.State.PendingGameplayEventBatches.RemoveAll(batch =>
            batchIds.Contains(batch.BatchId)
        );
        return sessionState.WriteStateAsync();
    }

    public async Task<GameSessionView> AbandonAsync(AbandonGameSessionCommand command)
    {
        var workingState = GameSessionGrainStateMapper.ToDomainState(sessionState.State);
        var view = GameSessionEngine.AbandonPlayer(
            workingState,
            this.GetPrimaryKey(),
            command,
            DateTime.UtcNow
        );
        sessionState.State = GameSessionGrainStateMapper.FromDomainState(
            workingState,
            sessionState.State.PendingGameplayEventBatches
        );
        await sessionState.WriteStateAsync();
        return view;
    }

    private static GameSessionGrainCommandResult MapCommandResult(
        GameSessionCommandResult result,
        Guid? pendingGameplayEventBatchId = null
    ) =>
        result switch
        {
            GameSessionCommandSucceededResult succeeded =>
                new GameSessionGrainCommandSucceededResult(
                    succeeded.Session,
                    succeeded.GameplayEvents,
                    pendingGameplayEventBatchId
                ),
            GameSessionCommandRejectedResult rejected => new GameSessionGrainCommandRejectedResult(
                rejected.ErrorMessage
            ),
            _ => throw new InvalidOperationException("Unknown game session command result type."),
        };
}
