using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Orleans;
using Spx.Contracts;
using Spx.Game.Application;
using Spx.Game.Application.Features.DeleteMessage;
using Spx.Game.Application.Features.EditMessage;
using Spx.Game.Application.Features.GetGamePresence;
using Spx.Game.Application.Features.GetMessages;
using Spx.Game.Application.Features.GetMessageUpdates;
using Spx.Game.Application.Features.LeaveGame;
using Spx.Game.Application.Features.SendPrivateMessage;
using Spx.Game.Application.Features.SendPublicMessage;
using Spx.Game.Application.Nexus;
using Spx.Game.Application.Nexus.Features.GetNexusPage;
using Spx.Game.Application.Nexus.Features.ManageDesign;
using Spx.Game.Application.Nexus.Features.SubmitOrders;
using Spx.Web.Components.Pages.Nexus;
using Spx.Web.Hubs;
using Spx.Web.Presence;
using Xunit;

namespace Spx.Web.Tests;

public sealed class NexusPageLifecycleTests : TestContext
{
    [Fact]
    public async Task DisposeAsync_revokes_presence_lease_when_coordinator_was_disposed_first()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var gameId = Guid.NewGuid();
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IGamePresenceGrain>();
        clusterClient.GetGrain<IGamePresenceGrain>(gameId).Returns(grain);
        var coordinator = new NexusPresenceLeaseCoordinator(clusterClient);
        var invalidationNotifier = Substitute.For<IGameInvalidationNotifier>();

        RegisterPageServices(gameId, coordinator, invalidationNotifier, clusterClient);

        var cut = RenderComponent<NexusPageHost>(parameters =>
            parameters
                .Add(x => x.GameId, gameId)
                .Add(x => x.AuthenticationStateTask, CreateAuthenticationStateTask("user-1"))
        );

        cut.WaitForAssertion(() =>
        {
            Assert.Single(
                grain.ReceivedCalls(),
                call => call.GetMethodInfo().Name == nameof(IGamePresenceGrain.RenewLeaseAsync)
            );
        });

        var renewCall = Assert.Single(
            grain.ReceivedCalls(),
            call => call.GetMethodInfo().Name == nameof(IGamePresenceGrain.RenewLeaseAsync)
        );
        var renewedLeaseId = Assert.IsType<Guid>(renewCall.GetArguments()[1]);

        coordinator.Dispose();

        await cut.FindComponent<NexusPage>().Instance.DisposeAsync();

        await invalidationNotifier
            .Received(1)
            .UnsubscribeAsync(gameId, Arg.Any<IGameInvalidationSubscriber>());
        await grain.Received(1).RevokeLeaseAsync(renewedLeaseId);
    }

    [Fact]
    public async Task OnPresenceChangedAsync_ignores_invalidations_for_other_games()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var gameId = Guid.NewGuid();
        var otherGameId = Guid.NewGuid();
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IGamePresenceGrain>();
        clusterClient.GetGrain<IGamePresenceGrain>(gameId).Returns(grain);
        var coordinator = new NexusPresenceLeaseCoordinator(clusterClient);
        var invalidationNotifier = Substitute.For<IGameInvalidationNotifier>();
        var pageHandler = new StubGetNexusPageHandler
        {
            Result = GamePageCoordinatorTestData.CreatePage(
                gameId,
                new GamePresenceView([GamePageCoordinatorTestData.CurrentPlayerId])
            ),
        };
        var presenceHandler = new StubGetGamePresenceHandler
        {
            Result = new GamePresenceView([GamePageCoordinatorTestData.CurrentPlayerId]),
        };

        RegisterPageServices(
            gameId,
            coordinator,
            invalidationNotifier,
            clusterClient,
            pageHandler,
            presenceHandler
        );

        var cut = RenderComponent<NexusPageHost>(parameters =>
            parameters
                .Add(x => x.GameId, gameId)
                .Add(x => x.AuthenticationStateTask, CreateAuthenticationStateTask("user-1"))
        );

        cut.WaitForAssertion(() => Assert.Equal(1, presenceHandler.CallCount));

        await cut.FindComponent<NexusPage>().Instance.OnPresenceChangedAsync(otherGameId);

        Assert.Equal(1, presenceHandler.CallCount);
    }

    [Fact]
    public async Task DisposeAsync_is_safe_when_circuit_closed_already_revoked_the_lease()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var gameId = Guid.NewGuid();
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IGamePresenceGrain>();
        clusterClient.GetGrain<IGamePresenceGrain>(gameId).Returns(grain);
        var coordinator = new NexusPresenceLeaseCoordinator(clusterClient);
        var invalidationNotifier = Substitute.For<IGameInvalidationNotifier>();

        RegisterPageServices(gameId, coordinator, invalidationNotifier, clusterClient);

        var cut = RenderComponent<NexusPageHost>(parameters =>
            parameters
                .Add(x => x.GameId, gameId)
                .Add(x => x.AuthenticationStateTask, CreateAuthenticationStateTask("user-1"))
        );

        cut.WaitForAssertion(() =>
        {
            Assert.Single(
                grain.ReceivedCalls(),
                call => call.GetMethodInfo().Name == nameof(IGamePresenceGrain.RenewLeaseAsync)
            );
        });

        var renewCall = Assert.Single(
            grain.ReceivedCalls(),
            call => call.GetMethodInfo().Name == nameof(IGamePresenceGrain.RenewLeaseAsync)
        );
        var renewedLeaseId = Assert.IsType<Guid>(renewCall.GetArguments()[1]);

        await coordinator.OnCircuitClosedAsync();
        await cut.FindComponent<NexusPage>().Instance.DisposeAsync();

        await invalidationNotifier
            .Received(1)
            .UnsubscribeAsync(gameId, Arg.Any<IGameInvalidationSubscriber>());
        await grain.Received(1).RevokeLeaseAsync(renewedLeaseId);
    }

    [Fact]
    public async Task OnGameStateChangedAsync_disconnects_when_reloaded_page_is_missing()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var gameId = Guid.NewGuid();
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IGamePresenceGrain>();
        clusterClient.GetGrain<IGamePresenceGrain>(gameId).Returns(grain);
        var coordinator = new NexusPresenceLeaseCoordinator(clusterClient);
        var invalidationNotifier = Substitute.For<IGameInvalidationNotifier>();
        var pageHandler = new StubGetNexusPageHandler
        {
            Result = GamePageCoordinatorTestData.CreatePage(
                gameId,
                new GamePresenceView([GamePageCoordinatorTestData.CurrentPlayerId])
            ),
        };

        RegisterPageServices(gameId, coordinator, invalidationNotifier, clusterClient, pageHandler);

        var cut = RenderComponent<NexusPageHost>(parameters =>
            parameters
                .Add(x => x.GameId, gameId)
                .Add(x => x.AuthenticationStateTask, CreateAuthenticationStateTask("user-1"))
        );

        cut.WaitForAssertion(() =>
        {
            Assert.Single(
                grain.ReceivedCalls(),
                call => call.GetMethodInfo().Name == nameof(IGamePresenceGrain.RenewLeaseAsync)
            );
        });

        var renewCall = Assert.Single(
            grain.ReceivedCalls(),
            call => call.GetMethodInfo().Name == nameof(IGamePresenceGrain.RenewLeaseAsync)
        );
        var renewedLeaseId = Assert.IsType<Guid>(renewCall.GetArguments()[1]);

        pageHandler.Result = null;

        await cut.FindComponent<NexusPage>().Instance.OnGameStateChangedAsync(gameId);

        await invalidationNotifier
            .Received(1)
            .UnsubscribeAsync(gameId, Arg.Any<IGameInvalidationSubscriber>());
        await grain.Received(1).RevokeLeaseAsync(renewedLeaseId);
        cut.WaitForAssertion(() => Assert.Contains("Game unavailable", cut.Markup));
    }

    private void RegisterPageServices(
        Guid gameId,
        NexusPresenceLeaseCoordinator coordinator,
        IGameInvalidationNotifier invalidationNotifier,
        IClusterClient clusterClient,
        StubGetNexusPageHandler? pageHandler = null,
        StubGetGamePresenceHandler? presenceHandler = null
    )
    {
        Services.AddAuthorizationCore();
        Services.AddSingleton<IGetNexusPageHandler>(
            pageHandler
                ?? new StubGetNexusPageHandler
                {
                    Result = GamePageCoordinatorTestData.CreatePage(
                        gameId,
                        new GamePresenceView([GamePageCoordinatorTestData.CurrentPlayerId])
                    ),
                }
        );
        Services.AddSingleton<IGetGamePresenceHandler>(
            presenceHandler
                ?? new StubGetGamePresenceHandler
                {
                    Result = new GamePresenceView([GamePageCoordinatorTestData.CurrentPlayerId]),
                }
        );
        Services.AddSingleton<IGetMessagesHandler>(
            new StubGetMessagesHandler { Result = new GameTimelinePageView([], false) }
        );
        Services.AddSingleton(Substitute.For<IGetMessageUpdatesHandler>());
        Services.AddSingleton(Substitute.For<ILeaveGameHandler>());
        Services.AddSingleton(Substitute.For<ISendPublicMessageHandler>());
        Services.AddSingleton(Substitute.For<ISendPrivateMessageHandler>());
        Services.AddSingleton(Substitute.For<IEditMessageHandler>());
        Services.AddSingleton(Substitute.For<IDeleteMessageHandler>());
        Services.AddSingleton(Substitute.For<ISubmitOrdersHandler>());
        Services.AddSingleton(Substitute.For<IManageDesignHandler>());
        Services.AddSingleton(clusterClient);
        Services.AddSingleton(invalidationNotifier);
        Services.AddSingleton(coordinator);
        Services.AddSingleton<ILogger<NexusPage>>(NullLogger<NexusPage>.Instance);
        Services.AddSingleton<ILogger<NexusPageActionCoordinator>>(
            NullLogger<NexusPageActionCoordinator>.Instance
        );
        Services.AddSingleton<ILogger<NexusPageDataCoordinator>>(
            NullLogger<NexusPageDataCoordinator>.Instance
        );
        Services.AddSingleton<ILogger<NexusPagePresenceCoordinator>>(
            NullLogger<NexusPagePresenceCoordinator>.Instance
        );
    }

    private static Task<AuthenticationState> CreateAuthenticationStateTask(string userId)
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "test")
        );

        return Task.FromResult(new AuthenticationState(principal));
    }

    private sealed class StubGetNexusPageHandler : IGetNexusPageHandler
    {
        public GamePageView? Result { get; set; }

        public Task<GamePageView?> HandleAsync(
            Guid gameId,
            string userId,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(Result);
    }

    private sealed class StubGetGamePresenceHandler : IGetGamePresenceHandler
    {
        public GamePresenceView Result { get; set; } = GamePresenceView.Empty;

        public int CallCount { get; private set; }

        public Task<GamePresenceView> HandleAsync(
            Guid gameId,
            CancellationToken cancellationToken = default
        )
        {
            CallCount++;
            return Task.FromResult(Result);
        }
    }

    private sealed class StubGetMessagesHandler : IGetMessagesHandler
    {
        public GameTimelinePageView? Result { get; init; }

        public Task<GameTimelinePageView?> HandleAsync(
            Guid gameId,
            Guid playerId,
            Guid? beforeMessageId = default,
            int take = GameMessageSupport.DefaultPageSize,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(Result);
    }

    private sealed class NexusPageHost : ComponentBase
    {
        [Parameter]
        public Guid GameId { get; set; }

        [Parameter]
        public Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<Task<AuthenticationState>>>(0);
            builder.AddAttribute(
                1,
                nameof(CascadingValue<Task<AuthenticationState>>.Value),
                AuthenticationStateTask
            );
            builder.AddAttribute(
                2,
                nameof(CascadingValue<Task<AuthenticationState>>.ChildContent),
                (RenderFragment)(
                    childBuilder =>
                    {
                        childBuilder.OpenComponent<NexusPage>(0);
                        childBuilder.AddAttribute(1, nameof(NexusPage.GameId), GameId);
                        childBuilder.CloseComponent();
                    }
                )
            );
            builder.CloseComponent();
        }
    }
}
