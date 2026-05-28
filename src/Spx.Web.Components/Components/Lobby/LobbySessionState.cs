using Spx.Game.Application;
using Spx.Game.Application.Nexus;

namespace Spx.Web.Components.Lobby;

public sealed class LobbySessionState
{
    public required GameLobbyView Lobby { get; init; }

    public NexusSessionView? Session { get; init; }

    public bool IsSubmittingGameplayAction { get; init; }

    public string? GameplayError { get; init; }
}
