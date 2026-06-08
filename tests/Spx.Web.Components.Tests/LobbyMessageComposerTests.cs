using AngleSharp.Html.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Spx.Game.Application;
using Spx.Web.Components.Lobby;
using Xunit;

namespace Spx.Web.Components.Tests;

public sealed class LobbyMessageComposerTests : TestContext
{
    [Fact]
    public void Typing_updates_visible_textarea_and_counter()
    {
        var currentPlayerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var cut = RenderComposer(currentPlayerId);

        cut.Find(TestIdSelector(LobbyMessageComposerTestIds.Composer))
            .Input("hello from the timeline");

        Assert.Contains("23 / 1024 characters", cut.Markup);
    }

    [Fact]
    public void Parent_rerender_without_reset_preserves_local_draft()
    {
        var currentPlayerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var cut = RenderComposer(currentPlayerId);

        cut.Find(TestIdSelector(LobbyMessageComposerTestIds.Composer))
            .Input("hello from the timeline");

        cut.SetParametersAndRender(parameters =>
            parameters.Add(x => x.State, MakeState(currentPlayerId, resetVersion: 0))
        );

        Assert.Contains("23 / 1024 characters", cut.Markup);
    }

    [Fact]
    public void Send_forwards_current_draft_and_recipient_payload()
    {
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

        cut.Find(TestIdSelector(LobbyMessageComposerTestIds.Composer))
            .Input("hello from the timeline");
        cut.Find(TestIdSelector(LobbyMessageComposerTestIds.Recipient))
            .Change(otherPlayerId.ToString());

        cut.Find(TestIdSelector(LobbyMessageComposerTestIds.SendButton)).Click();

        Assert.Equal(
            new LobbyMessageComposerSubmitRequest(
                "hello from the timeline",
                otherPlayerId.ToString()
            ),
            submitted
        );
    }

    [Fact]
    public void Send_with_no_recipient_sends_null_recipient()
    {
        var currentPlayerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        LobbyMessageComposerSubmitRequest? submitted = null;

        var cut = RenderComposer(
            currentPlayerId,
            request =>
            {
                submitted = request;
                return Task.CompletedTask;
            }
        );

        cut.Find(TestIdSelector(LobbyMessageComposerTestIds.Composer)).Input("broadcast message");

        cut.Find(TestIdSelector(LobbyMessageComposerTestIds.SendButton)).Click();

        Assert.Equal(new LobbyMessageComposerSubmitRequest("broadcast message", null), submitted);
    }

    [Fact]
    public void IsSendingMessage_suppresses_send_and_shows_sending_text()
    {
        var currentPlayerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var sendCount = 0;

        var cut = RenderComposer(
            currentPlayerId,
            _ =>
            {
                sendCount++;
                return Task.CompletedTask;
            },
            isSendingMessage: true
        );

        cut.Find(TestIdSelector(LobbyMessageComposerTestIds.Composer)).Input("hello");
        cut.Find(TestIdSelector(LobbyMessageComposerTestIds.SendButton)).Click();

        Assert.Equal(0, sendCount);
        Assert.Contains("Sending...", cut.Markup);
    }

    [Fact]
    public void Inactive_user_shows_disabled_composer_state()
    {
        var currentPlayerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var cut = RenderComponent<LobbyMessageComposer>(parameters =>
            parameters
                .Add(x => x.State, MakeState(currentPlayerId, isCurrentUserActive: false))
                .Add(
                    x => x.OnSend,
                    EventCallback.Factory.Create<LobbyMessageComposerSubmitRequest>(this, NoOpSend)
                )
        );

        Assert.Contains("no longer an active player", cut.Markup);
        Assert.Empty(cut.FindAll(TestIdSelector(LobbyMessageComposerTestIds.Composer)));
    }

    [Fact]
    public void Reset_version_change_clears_visible_textarea_and_recipient()
    {
        var currentPlayerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var otherPlayerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var cut = RenderComposer(currentPlayerId);

        cut.Find(TestIdSelector(LobbyMessageComposerTestIds.Composer))
            .Input("hello from the timeline");
        cut.Find(TestIdSelector(LobbyMessageComposerTestIds.Recipient))
            .Change(otherPlayerId.ToString());

        cut.SetParametersAndRender(parameters =>
            parameters.Add(x => x.State, MakeState(currentPlayerId, resetVersion: 1))
        );

        var composer = (IHtmlTextAreaElement)
            cut.Find(TestIdSelector(LobbyMessageComposerTestIds.Composer));
        var recipient = (IHtmlSelectElement)
            cut.Find(TestIdSelector(LobbyMessageComposerTestIds.Recipient));

        Assert.Equal(string.Empty, composer.Value);
        Assert.Equal(string.Empty, recipient.Value);
        Assert.Contains("0 / 1024 characters", cut.Markup);
    }

    private IRenderedComponent<LobbyMessageComposer> RenderComposer(
        Guid currentPlayerId,
        Func<LobbyMessageComposerSubmitRequest, Task>? onSend = null,
        bool isSendingMessage = false
    )
    {
        Func<LobbyMessageComposerSubmitRequest, Task> handler = onSend ?? NoOpSend;
        return RenderComponent<LobbyMessageComposer>(parameters =>
            parameters
                .Add(x => x.State, MakeState(currentPlayerId, isSendingMessage: isSendingMessage))
                .Add(
                    x => x.OnSend,
                    EventCallback.Factory.Create<LobbyMessageComposerSubmitRequest>(this, handler)
                )
        );
    }

    private static string TestIdSelector(string testId) => $"[data-testid='{testId}']";

    private static LobbyMessagesState MakeState(
        Guid currentPlayerId,
        bool isCurrentUserActive = true,
        bool isSendingMessage = false,
        int resetVersion = 0
    ) =>
        new()
        {
            CurrentUserName = "Captain Red",
            IsCurrentUserActive = isCurrentUserActive,
            CurrentPlayerId = currentPlayerId,
            Players = CreatePlayers(currentPlayerId),
            ComposerResetVersion = resetVersion,
            IsSendingMessage = isSendingMessage,
        };

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
