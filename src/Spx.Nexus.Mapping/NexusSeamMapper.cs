using System.Collections.Immutable;
using Spx.Game.Application;
using Spx.Game.Application.Nexus;
using Spx.Nexus.Domain;
using Spx.Nexus.Primitives;

namespace Spx.Nexus.Mapping;

public static class NexusSeamMapper
{
    public static NexusTurnOrdersCommand ToDomain(NexusSubmitTurnCommand command) =>
        new(
            command.PlayerId,
            command.ExpectedRoundNumber,
            [
                .. command.MoveOrders.Select(move => new NexusMoveOrder(
                    move.From,
                    move.To,
                    move.Units
                )),
            ],
            [
                .. command.BuildOrders.Select(build => new NexusBuildOrder(
                    build.UnitType,
                    build.Count
                )),
            ],
            command.BeginNexusGate
        );

    public static NexusSessionView ToApplication(NexusGameView view) =>
        new(
            view.GameId,
            view.RoundNumber,
            [.. view.Systems.Select(ToApplication)],
            ToApplication(view.CurrentPlayer),
            ToApplication(view.Opponent),
            [.. view.LastResolveEvents.Select(ToApplication)],
            view.Completion is null
                ? null
                : new NexusSessionCompletion(
                    view.Completion.Outcome == NexusGameOutcome.Victory
                        ? NexusSessionOutcome.Victory
                        : NexusSessionOutcome.Draw,
                    view.Completion.WinnerId
                )
        );

    public static NexusSystemSnapshot ToApplication(NexusSystemView system) =>
        new(
            system.Coord,
            system.IsNexus,
            system.IncomeValue,
            system.HomePlayerId,
            system.ControlOwner,
            system.Units
        );

    public static NexusPlayerSnapshot ToApplication(NexusPlayerView player) =>
        new(
            player.PlayerId,
            player.Faction,
            player.Energy,
            player.GateProgress,
            player.HasSubmittedOrders,
            player.IsActive,
            player
                .PendingMoveOrders?.Select(move => new NexusMoveRequest(
                    move.From,
                    move.To,
                    move.Units
                ))
                .ToImmutableArray(),
            player
                .PendingBuildOrders?.Select(build => new NexusBuildRequest(
                    build.UnitType,
                    build.Count
                ))
                .ToImmutableArray(),
            player.PendingBeginNexusGate,
            player.SupplyPool,
            player.CapitalCount
        );

    public static NexusSessionEvent ToApplication(NexusResolveEvent resolveEvent) =>
        resolveEvent switch
        {
            NexusUnitsMovedEvent moved => new NexusUnitsMovedSessionEvent(
                moved.PlayerId,
                moved.From,
                moved.To,
                moved.Units,
                moved.IsRetreat
            ),
            NexusPlanetaryControlEvent control => new NexusPlanetaryControlSessionEvent(
                control.System,
                control.PlayerId
            ),
            NexusSystemContestedEvent contested => new NexusSystemContestedSessionEvent(
                contested.System
            ),
            NexusSystemUncontrolledEvent uncontrolled => new NexusSystemUncontrolledSessionEvent(
                uncontrolled.System
            ),
            NexusCombatBeganEvent combat => new NexusCombatBeganSessionEvent(
                combat.System,
                combat.Player1Id,
                combat.Player2Id
            ),
            NexusPhaseResultEvent phaseResult => new NexusPhaseResultSessionEvent(
                phaseResult.System
            ),
            NexusSystemClearedEvent cleared => new NexusSystemClearedSessionEvent(
                cleared.System,
                cleared.VictorId
            ),
            NexusIncomeEvent income => new NexusIncomeSessionEvent(income.PlayerId, income.Sources),
            NexusUnitDeployedEvent deployed => new NexusUnitDeployedSessionEvent(
                deployed.PlayerId,
                deployed.UnitType,
                deployed.HomeSystem,
                deployed.Count
            ),
            NexusGateStartedEvent started => new NexusGateStartedSessionEvent(
                started.PlayerId,
                started.System
            ),
            NexusGateCompletedEvent completed => new NexusGateCompletedSessionEvent(
                completed.PlayerId,
                completed.System
            ),
            NexusGateCancelledEvent cancelled => new NexusGateCancelledSessionEvent(
                cancelled.PlayerId,
                cancelled.System
            ),
            NexusCapitalDisbandedEvent disbanded => new NexusCapitalDisbandedSessionEvent(
                disbanded.PlayerId,
                disbanded.UnitType,
                disbanded.System,
                disbanded.Count
            ),
            NexusVictoryEvent victory => new NexusVictorySessionEvent(victory.WinnerId),
            NexusDrawEvent draw => new NexusDrawSessionEvent(draw.Reason),
            _ => new NexusUnknownSessionEvent(),
        };
}
