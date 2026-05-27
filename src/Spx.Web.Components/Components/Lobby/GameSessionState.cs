using Spx.Game.Application;
using Spx.Game.Domain;

namespace Spx.Web.Components.Lobby;

public sealed class GameSessionState
{
    public required GameLobbyView Lobby { get; init; }

    public NexusGameView? Session { get; init; }

    public bool IsSubmittingGameplayAction { get; init; }

    public string? GameplayError { get; init; }
}
