namespace Spx.Game.Application.Features.SubmitOrders;

internal sealed class SubmitOrdersHandler(
    IGameSessionService gameSessionService,
    IGameSessionInvalidationPublisher sessionInvalidationPublisher,
    IGameMessageInvalidationPublisher messageInvalidationPublisher,
    IGamePersistence persistence,
    IGameMessagePersistence messagesPersistence
) : ISubmitOrdersHandler
{
    public async Task<GameSessionCommandOutcome> HandleAsync(
        Guid gameId,
        NexusTurnOrdersCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var outcome = await gameSessionService.SubmitOrdersAsync(
            gameId,
            command,
            cancellationToken
        );

        if (outcome is GameSessionCommandSucceeded { Session: { } session })
        {
            await sessionInvalidationPublisher.PublishSessionInvalidatedAsync(
                gameId,
                cancellationToken
            );

            if (session.ResolveEvents.Length > 0)
            {
                await WriteResolveEventsAsync(gameId, session, cancellationToken);
                await messageInvalidationPublisher.PublishMessagesInvalidatedAsync(
                    gameId,
                    cancellationToken
                );
            }
        }

        return outcome;
    }

    private async Task WriteResolveEventsAsync(
        Guid gameId,
        NexusGameView session,
        CancellationToken cancellationToken
    )
    {
        var players = await persistence.GetActivePlayersAsync(gameId, cancellationToken);

        var redPlayer =
            session.CurrentPlayer.Faction == NexusFactionColor.Red
                ? session.CurrentPlayer
                : session.OpponentPlayer;
        var bluePlayer =
            session.CurrentPlayer.Faction == NexusFactionColor.Blue
                ? session.CurrentPlayer
                : session.OpponentPlayer;

        var redName = players.FirstOrDefault(p => p.PlayerId == redPlayer.PlayerId)?.Name ?? "Red";
        var blueName =
            players.FirstOrDefault(p => p.PlayerId == bluePlayer.PlayerId)?.Name ?? "Blue";

        var bodies = session
            .ResolveEvents.Select(evt =>
                NexusResolveEventMessageFormatter.Format(evt, redName, blueName)
            )
            .ToList();

        await messagesPersistence.WriteGameplayEventsAsync(gameId, bodies, cancellationToken);
    }
}
