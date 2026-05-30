namespace Spx.Web.Components.Lobby;

public sealed record LobbyMessageComposerSubmitRequest(
    string Text,
    string? SelectedRecipientPlayerIdString
);
