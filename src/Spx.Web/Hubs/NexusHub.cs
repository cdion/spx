using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Orleans;
using Spx.Contracts;

namespace Spx.Web.Hubs;

[Authorize]
public sealed class NexusHub(
    IClusterClient clusterClient,
    IGameInvalidationHubBridge bridge,
    INexusHubAccessService accessService
) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var query = Context.GetHttpContext()?.Request.Query;

        if (!Guid.TryParse(query?["gameId"], out var gameId))
        {
            Context.Abort();
            return;
        }

        var user = Context.User ?? new ClaimsPrincipal();
        var access = await accessService.GetAccessAsync(gameId, user, Context.ConnectionAborted);
        if (access is null)
        {
            Context.Abort();
            return;
        }

        Context.Items["gameId"] = gameId;
        Context.Items["playerId"] = access.PlayerId;
        Context.Items["isActivePlayer"] = access.IsActivePlayer;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"game:{gameId}");

        if (access.IsActivePlayer)
        {
            await clusterClient
                .GetGrain<IGamePresenceGrain>(gameId)
                .SetOnlineAsync(access.PlayerId);
            await Clients.Group($"game:{gameId}").SendAsync("PresenceChanged", gameId);
        }

        await bridge.OnGameConnectedAsync(gameId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (
            Context.Items.TryGetValue("gameId", out var rawGameId)
            && Context.Items.TryGetValue("playerId", out var rawPlayerId)
            && Context.Items.TryGetValue("isActivePlayer", out var rawIsActivePlayer)
            && rawGameId is Guid gameId
            && rawPlayerId is Guid playerId
            && rawIsActivePlayer is bool isActivePlayer
        )
        {
            if (isActivePlayer)
            {
                await clusterClient.GetGrain<IGamePresenceGrain>(gameId).SetOfflineAsync(playerId);
                await Clients.Group($"game:{gameId}").SendAsync("PresenceChanged", gameId);
            }

            await bridge.OnGameDisconnectedAsync(gameId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
