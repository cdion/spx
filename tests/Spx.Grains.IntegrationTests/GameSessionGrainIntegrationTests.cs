using Orleans;
using Spx.Contracts;
using Xunit;

namespace Spx.Grains.IntegrationTests;

[Collection(OrleansClusterCollection.Name)]
public sealed class GameSessionGrainIntegrationTests(OrleansClusterFixture fixture)
{
    private static readonly Guid GameId = Guid.Parse("a4ad04ba-7717-4684-bb8c-7827b2c01fb9");
    private static readonly GameSessionParticipantGrainView FirstPlayer = new(Guid.Parse("85b56bc8-bd95-48f5-8374-f53714734717"));
    private static readonly GameSessionParticipantGrainView SecondPlayer = new(Guid.Parse("6421fe5a-5585-4db9-b48b-e6caf8323b8f"));

    [Fact]
    public async Task SessionState_survives_deactivation_and_rejoin_view_load()
    {
        var grain = fixture.Cluster.Client.GetGrain<IGameSessionGrain>(GameId);

        await grain.InitializeAsync(new InitializeGameSessionGrainCommand(FirstPlayer, SecondPlayer));

        for (var pick = 0; pick < 4; pick++)
        {
            var currentView = await grain.GetPlayerViewAsync(new GetGameSessionGrainQuery(FirstPlayer.PlayerId));
            Assert.NotNull(currentView);

            var currentPlayerId = currentView!.CanAcquireCard
                ? FirstPlayer.PlayerId
                : SecondPlayer.PlayerId;
            var pickedCardId = currentView.CanAcquireCard
                ? currentView.VisibleMarketCards[0].CardInstanceId
                : (await grain.GetPlayerViewAsync(new GetGameSessionGrainQuery(SecondPlayer.PlayerId)))!.VisibleMarketCards[0].CardInstanceId;

            await grain.SubmitAcquireAsync(new SubmitAcquireGrainCommand(currentPlayerId, currentView.RoundNumber, pickedCardId));
        }

        var firstPlayView = await grain.GetPlayerViewAsync(new GetGameSessionGrainQuery(FirstPlayer.PlayerId));
        Assert.NotNull(firstPlayView);
        Assert.Equal(GamePhase.Play, firstPlayView!.Phase);

        await grain.SubmitPlayBatchAsync(new SubmitPlayBatchGrainCommand(FirstPlayer.PlayerId, firstPlayView.RoundNumber, []));
        await grain.SubmitPlayBatchAsync(new SubmitPlayBatchGrainCommand(SecondPlayer.PlayerId, firstPlayView.RoundNumber, []));

        var progressedView = await grain.GetPlayerViewAsync(new GetGameSessionGrainQuery(FirstPlayer.PlayerId));
        Assert.NotNull(progressedView);
        Assert.Equal(2, progressedView!.RoundNumber);

        await Task.Delay(TimeSpan.FromSeconds(4));

        var rejoinedView = await grain.GetPlayerViewAsync(new GetGameSessionGrainQuery(FirstPlayer.PlayerId));
        Assert.NotNull(rejoinedView);
        Assert.Equal(2, rejoinedView!.RoundNumber);
    }
}