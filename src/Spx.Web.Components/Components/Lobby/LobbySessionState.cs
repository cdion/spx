using Spx.Game.Application;
using Spx.Game.Application.Nexus;
using Spx.Nexus.Domain;

namespace Spx.Web.Components.Lobby;

public sealed class LobbySessionState
{
    public required GameLobbyView Lobby { get; init; }

    public NexusGameView? Session { get; init; }

    public bool IsSubmittingGameplayAction { get; init; }

    public string? GameplayError { get; init; }
}
