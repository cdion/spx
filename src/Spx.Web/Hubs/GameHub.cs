using Microsoft.AspNetCore.SignalR;
using Orleans;
using Spx.Contracts;

namespace Spx.Web.Hubs;

public sealed class GameHub(IClusterClient clusterClient, IGameInvalidationHubBridge bridge) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var query = Context.GetHttpContext()?.Request.Query;

        if (
            !Guid.TryParse(query?["gameId"], out var gameId)
            || !Guid.TryParse(query?["playerId"], out var playerId)
        )
        {
            Context.Abort();
            return;
        }

        Context.Items["gameId"] = gameId;
        Context.Items["playerId"] = playerId;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"game:{gameId}");
        await clusterClient.GetGrain<IGamePresenceGrain>(gameId).SetOnlineAsync(playerId);
        await Clients.Group($"game:{gameId}").SendAsync("PresenceChanged", gameId);
        await bridge.OnGameConnectedAsync(gameId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (
            Context.Items.TryGetValue("gameId", out var rawGameId)
            && Context.Items.TryGetValue("playerId", out var rawPlayerId)
            && rawGameId is Guid gameId
            && rawPlayerId is Guid playerId
        )
        {
            await clusterClient.GetGrain<IGamePresenceGrain>(gameId).SetOfflineAsync(playerId);
            await Clients.Group($"game:{gameId}").SendAsync("PresenceChanged", gameId);
            await bridge.OnGameDisconnectedAsync(gameId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
