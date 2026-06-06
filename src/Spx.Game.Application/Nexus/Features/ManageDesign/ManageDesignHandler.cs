using Spx.Nexus.Domain;

namespace Spx.Game.Application.Nexus.Features.ManageDesign;

internal sealed class ManageDesignHandler(
    INexusSessionService gameSessionService,
    INexusSessionInvalidationPublisher sessionInvalidationPublisher
) : IManageDesignHandler
{
    public async Task<GameSessionCommandOutcome> CreateDesignAsync(
        Guid gameId,
        NexusCreateDesignCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var outcome = await gameSessionService.CreateDesignAsync(
            gameId,
            command,
            cancellationToken
        );

        if (outcome is GameSessionCommandSucceeded)
        {
            await sessionInvalidationPublisher.PublishSessionInvalidatedAsync(
                gameId,
                cancellationToken
            );
        }

        return outcome;
    }

    public async Task<GameSessionCommandOutcome> DeleteDesignAsync(
        Guid gameId,
        NexusDeleteDesignCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var outcome = await gameSessionService.DeleteDesignAsync(
            gameId,
            command,
            cancellationToken
        );

        if (outcome is GameSessionCommandSucceeded)
        {
            await sessionInvalidationPublisher.PublishSessionInvalidatedAsync(
                gameId,
                cancellationToken
            );
        }

        return outcome;
    }
}
