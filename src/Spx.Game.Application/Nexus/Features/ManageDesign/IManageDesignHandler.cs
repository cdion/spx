using Spx.Nexus.Domain;

namespace Spx.Game.Application.Nexus.Features.ManageDesign;

public interface IManageDesignHandler
{
    Task<GameSessionCommandOutcome> CreateDesignAsync(
        Guid gameId,
        NexusCreateDesignCommand command,
        CancellationToken cancellationToken = default
    );

    Task<GameSessionCommandOutcome> DeleteDesignAsync(
        Guid gameId,
        NexusDeleteDesignCommand command,
        CancellationToken cancellationToken = default
    );
}
