using AngleSharp.Html.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Spx.Game.Application;
using Spx.Web.Components.Lobby;
using Xunit;

namespace Spx.Web.Tests;

public sealed class LobbyMessageComposerTests : TestContext
{
    [Fact]
    public void Typing_updates_visible_textarea_and_counter()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var currentPlayerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var cut = RenderComposer(currentPlayerId);

        cut.Find("#message-composer").Input("hello from the timeline");

        Assert.Contains("23 / 1024 characters", cut.Markup);
    }

    [Fact]
    public void Parent_rerender_without_reset_preserves_local_draft()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var currentPlayerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var cut = RenderComposer(currentPlayerId);

        cut.Find("#message-composer").Input("hello from the timeline");

        cut.SetParametersAndRender(parameters =>
            parameters
                .Add(x => x.IsCurrentUserActive, true)
                .Add(x => x.CurrentPlayerId, currentPlayerId)
                .Add(x => x.Players, CreatePlayers(currentPlayerId))
                .Add(x => x.ComposerResetVersion, 0)
                .Add(x => x.IsSendingMessage, false)
        );

        Assert.Contains("23 / 1024 characters", cut.Markup);
    }

    [Fact]
    public void Send_forwards_current_draft_and_recipient_payload()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var currentPlayerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var otherPlayerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        LobbyMessageComposerSubmitRequest? submitted = null;

        var cut = RenderComposer(
            currentPlayerId,
            request =>
            {
                submitted = request;
                return Task.CompletedTask;
            }
        );

        cut.Find("#message-composer").Input("hello from the timeline");
        cut.Find("#message-recipient").Change(otherPlayerId.ToString());

        cut.Find("button.ui-button-primary").Click();

        Assert.Equal(
            new LobbyMessageComposerSubmitRequest(
                "hello from the timeline",
                otherPlayerId.ToString()
            ),
            submitted
        );
    }

    [Fact]
    public void Reset_version_change_clears_visible_textarea_and_recipient()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var currentPlayerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var otherPlayerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var cut = RenderComposer(currentPlayerId);

        cut.Find("#message-composer").Input("hello from the timeline");
        cut.Find("#message-recipient").Change(otherPlayerId.ToString());

        cut.SetParametersAndRender(parameters =>
            parameters
                .Add(x => x.IsCurrentUserActive, true)
                .Add(x => x.CurrentPlayerId, currentPlayerId)
                .Add(x => x.Players, CreatePlayers(currentPlayerId))
                .Add(x => x.ComposerResetVersion, 1)
                .Add(x => x.IsSendingMessage, false)
        );

        var composer = (IHtmlTextAreaElement)cut.Find("#message-composer");
        var recipient = (IHtmlSelectElement)cut.Find("#message-recipient");

        Assert.Equal(string.Empty, composer.Value);
        Assert.Equal(string.Empty, recipient.Value);
        Assert.Contains("0 / 1024 characters", cut.Markup);
    }

    private IRenderedComponent<LobbyMessageComposer> RenderComposer(
        Guid currentPlayerId,
        Func<LobbyMessageComposerSubmitRequest, Task>? onSend = null
    )
    {
        Func<LobbyMessageComposerSubmitRequest, Task> handler = onSend ?? NoOpSend;
        return RenderComponent<LobbyMessageComposer>(parameters =>
            parameters
                .Add(x => x.IsCurrentUserActive, true)
                .Add(x => x.CurrentPlayerId, currentPlayerId)
                .Add(x => x.Players, CreatePlayers(currentPlayerId))
                .Add(x => x.ComposerResetVersion, 0)
                .Add(x => x.IsSendingMessage, false)
                .Add(
                    x => x.OnSend,
                    EventCallback.Factory.Create<LobbyMessageComposerSubmitRequest>(this, handler)
                )
        );
    }

    private static Task NoOpSend(LobbyMessageComposerSubmitRequest _) => Task.CompletedTask;

    private static IReadOnlyList<GamePlayerView> CreatePlayers(Guid currentPlayerId) =>
        [
            new(currentPlayerId, "Captain Red", DateTime.UtcNow),
            new(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "Captain Blue",
                DateTime.UtcNow
            ),
        ];
}
