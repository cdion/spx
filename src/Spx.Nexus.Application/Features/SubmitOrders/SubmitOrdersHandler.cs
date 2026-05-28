namespace Spx.Nexus.Application.Features.SubmitOrders;

internal sealed class SubmitOrdersHandler(
    INexusSessionService gameSessionService,
    INexusSessionInvalidationPublisher sessionInvalidationPublisher,
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
            if (session.LastResolveEvents.Length > 0)
            {
                await WriteResolveEventsAsync(gameId, session, cancellationToken);
            }

            await sessionInvalidationPublisher.PublishSessionInvalidatedAsync(
                gameId,
                cancellationToken
            );

            if (session.LastResolveEvents.Length > 0)
            {
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

        var allSessionPlayers = new[] { session.CurrentPlayer, session.Opponent };
        var playerNames = allSessionPlayers.ToDictionary(
            p => p.PlayerId,
            p => players.FirstOrDefault(x => x.PlayerId == p.PlayerId)?.Name ?? p.Faction.ToString()
        );

        var bodies = session
            .LastResolveEvents.Select(evt =>
                NexusResolveEventMessageFormatter.Format(evt, playerNames)
            )
            .ToList();

        await messagesPersistence.WriteGameplayEventsAsync(gameId, bodies, cancellationToken);
    }
}
