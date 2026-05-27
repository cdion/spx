using System.Collections.Immutable;

namespace Spx.Game.Domain;

public static class NexusGameEngine
{
    private const int GateCost = 12;

    // ── Public API ────────────────────────────────────────────────────────────

    public static void Initialize(
        NexusGameState state,
        InitializeNexusGameCommand command,
        Random rng
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(rng);

        if (command.Players.Length != 2)
            throw new InvalidOperationException("Nexus Protocol requires exactly 2 players.");

        if (command.Players.Select(p => p.PlayerId).Distinct().Count() != 2)
            throw new InvalidOperationException("All players must have distinct IDs.");

        state.RoundNumber = 1;
        state.Completion = null;
        state.LastResolveEvents = [];

        var factions = new[] { NexusFactionColor.Red, NexusFactionColor.Blue };
        state.Players = command
            .Players.Select(
                (p, i) =>
                    new NexusPlayerState
                    {
                        PlayerId = p.PlayerId,
                        Faction = factions[i],
                        Energy = 0,
                        IsActive = true,
                    }
            )
            .ToList();

        state.Systems = NexusMap.GenerateMap(
            command.Players[0].PlayerId,
            command.Players[1].PlayerId,
            rng
        );
    }

    public static NexusTurnOrdersResult SubmitOrders(
        NexusGameState state,
        NexusTurnOrdersCommand command,
        Random rng
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(rng);

        if (state.Completion is not null)
            return new NexusTurnOrdersRejected("Game is already over.");
        if (command.ExpectedRoundNumber != state.RoundNumber)
            return new NexusTurnOrdersRejected(
                $"Round mismatch: submitted {command.ExpectedRoundNumber}, current is {state.RoundNumber}."
            );

        var player = GetPlayer(state, command.PlayerId);
        if (player is null)
            return new NexusTurnOrdersRejected("Player not found.");
        if (!player.IsActive)
            return new NexusTurnOrdersRejected("Player is not active.");
        if (player.HasSubmittedOrders)
            return new NexusTurnOrdersRejected("Orders already submitted this round.");

        var error =
            ValidateMoveOrders(state, command.PlayerId, command.MoveOrders)
            ?? ValidateBuildAndGate(state, player, command);
        if (error is not null)
            return error;

        player.PendingMoveOrders = [.. command.MoveOrders];
        player.PendingBuildOrders = [.. command.BuildOrders];
        player.PendingBeginNexusGate = command.BeginNexusGate;
        player.HasSubmittedOrders = true;

        if (state.Players.All(p => !p.IsActive || p.HasSubmittedOrders))
            Resolve(state, rng);

        return new NexusTurnOrdersAccepted();
    }

    public static void Abandon(NexusGameState state, Guid playerId)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Completion is not null)
            return;

        var player = GetPlayer(state, playerId);
        if (player is null)
            return;

        player.IsActive = false;

        var remaining = state.Players.Where(p => p.IsActive).ToList();
        if (remaining.Count == 1)
        {
            state.Completion = new NexusGameCompletion(
                NexusGameOutcome.Victory,
                remaining[0].PlayerId
            );
        }
    }

    public static NexusGameView BuildView(NexusGameState state, Guid gameId, Guid playerId)
    {
        ArgumentNullException.ThrowIfNull(state);

        var current =
            GetPlayer(state, playerId)
            ?? throw new InvalidOperationException($"Player {playerId} not found.");
        var opponent = state.Players.First(p => p.PlayerId != playerId);

        var systems = state
            .Systems.Select(s => new NexusSystemView(
                s.Coord,
                s.IsNexus,
                s.IncomeValue,
                s.HomePlayerId,
                s.ControlOwner,
                s.Units.ToImmutableDictionary(
                    outer => outer.Key,
                    outer => outer.Value.ToImmutableDictionary()
                )
            ))
            .ToImmutableArray();

        return new NexusGameView(
            gameId,
            state.RoundNumber,
            systems,
            ProjectPlayerView(current, isSelf: true),
            ProjectPlayerView(opponent, isSelf: false),
            state.LastResolveEvents.ToImmutableArray(),
            state.Completion
        );
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private static NexusTurnOrdersRejected? ValidateMoveOrders(
        NexusGameState state,
        Guid playerId,
        IEnumerable<NexusMoveOrder> orders
    )
    {
        var committed = new Dictionary<HexCoord, Dictionary<NexusUnitType, int>>();

        foreach (var order in orders)
        {
            if (!NexusMap.IsValidCoord(order.From))
                return new NexusTurnOrdersRejected(
                    $"Source system {order.From} is not on the map."
                );
            if (!NexusMap.IsValidCoord(order.To))
                return new NexusTurnOrdersRejected(
                    $"Destination system {order.To} is not on the map."
                );
            if (!NexusMap.AreAdjacent(order.From, order.To))
                return new NexusTurnOrdersRejected($"{order.To} is not adjacent to {order.From}.");
            if (order.Units.Count == 0)
                return new NexusTurnOrdersRejected("A move order must include at least one unit.");

            var system = GetSystem(state, order.From);
            if (system is null)
                return new NexusTurnOrdersRejected($"Source system {order.From} not found.");

            if (!committed.TryGetValue(order.From, out var fromCommitted))
            {
                fromCommitted = [];
                committed[order.From] = fromCommitted;
            }

            var capacityProvided = 0;
            var capacityNeeded = 0;

            foreach (var (unitType, count) in order.Units)
            {
                if (count <= 0)
                    return new NexusTurnOrdersRejected(
                        $"Unit count for {unitType} must be positive."
                    );

                var alreadyCommitted = fromCommitted.GetValueOrDefault(unitType);
                var available = system.GetUnitCount(playerId, unitType);

                if (alreadyCommitted + count > available)
                    return new NexusTurnOrdersRejected(
                        $"Insufficient {unitType} at {order.From}: "
                            + $"need {alreadyCommitted + count}, have {available}."
                    );

                fromCommitted[unitType] = alreadyCommitted + count;
                capacityProvided += unitType.CarryCapacity() * count;
                capacityNeeded += unitType.ConsumedCapacity() * count;
            }

            if (capacityNeeded > capacityProvided)
                return new NexusTurnOrdersRejected(
                    $"Insufficient carry capacity for move from {order.From} to {order.To}: "
                        + $"need {capacityNeeded} slots, have {capacityProvided}."
                );
        }

        return null;
    }

    private static NexusTurnOrdersRejected? ValidateBuildAndGate(
        NexusGameState state,
        NexusPlayerState player,
        NexusTurnOrdersCommand command
    )
    {
        var buildCost = command.BuildOrders.Sum(o => o.UnitType.Cost() * o.Count);
        var gateCost = command.BeginNexusGate ? GateCost : 0;

        if (player.Energy < buildCost + gateCost)
            return new NexusTurnOrdersRejected(
                $"Insufficient Energy: need {buildCost + gateCost}, have {player.Energy}."
            );

        if (command.BeginNexusGate)
        {
            if (player.GateProgress == NexusGateProgress.Completed)
                return new NexusTurnOrdersRejected("Nexus Gate is already completed.");

            var nexusSystem = GetSystem(state, NexusMap.NexusCoord);
            if (nexusSystem is null || !nexusSystem.HasPlanetaryUnits(player.PlayerId))
                return new NexusTurnOrdersRejected(
                    "Cannot begin Nexus Gate: no planetary units on the Nexus."
                );
        }

        return null;
    }

    // ── Resolution ────────────────────────────────────────────────────────────

    private static void Resolve(NexusGameState state, Random rng)
    {
        var events = new List<NexusResolveEvent>();
        var players = state.Players.Where(p => p.IsActive).ToList();
        var player1 = players[0];
        var player2 = players[1];

        // Step 1: Deduct build costs and gate payments
        foreach (var player in players)
        {
            var buildCost = player.PendingBuildOrders.Sum(o => o.UnitType.Cost() * o.Count);
            var gateCost = player.PendingBeginNexusGate ? GateCost : 0;
            player.Energy -= buildCost + gateCost;
        }

        // Step 2: Apply moves
        ApplyMoves(state, player1, player2, events);

        // Step 3: Combat
        ResolveCombat(state, player1.PlayerId, player2.PlayerId, events, rng);

        // Step 4: Income
        foreach (var player in players)
        {
            var sources = state.Systems.Where(s => s.ControlOwner == player.PlayerId).ToList();
            var income = sources.Sum(s => s.IncomeValue);
            if (income > 0)
            {
                player.Energy += income;
                events.Add(
                    new NexusIncomeEvent(
                        player.PlayerId,
                        income,
                        sources.Select(s => s.Coord).ToImmutableArray()
                    )
                );
            }
        }

        // Step 5: Deploy built units to home system
        foreach (var player in players)
        {
            var home = state.Systems.FirstOrDefault(s => s.HomePlayerId == player.PlayerId);
            if (home is null)
                continue;

            foreach (var order in player.PendingBuildOrders)
            {
                home.AddUnits(player.PlayerId, order.UnitType, order.Count);
                events.Add(
                    new NexusUnitDeployedEvent(
                        player.PlayerId,
                        order.UnitType,
                        home.Coord,
                        order.Count
                    )
                );
            }
        }

        // Step 6: Gate progress and win check
        var completedIds = new List<Guid>();
        foreach (var player in players)
        {
            var nexus = GetSystem(state, NexusMap.NexusCoord);
            var hasPlanetaryOnNexus = nexus is not null && nexus.HasPlanetaryUnits(player.PlayerId);

            if (player.PendingBeginNexusGate && !hasPlanetaryOnNexus)
            {
                // Committed this turn but planetary units lost or never arrived — cancel and refund
                player.Energy += GateCost;
                if (player.GateProgress == NexusGateProgress.Started)
                    player.GateProgress = NexusGateProgress.None;
                events.Add(new NexusGateCancelledEvent(player.PlayerId, NexusMap.NexusCoord));
            }
            else if (
                player.GateProgress == NexusGateProgress.Started
                && !player.PendingBeginNexusGate
            )
            {
                // Was building but didn't commit this turn — cancel (no refund, energy spent)
                player.GateProgress = NexusGateProgress.None;
                events.Add(new NexusGateCancelledEvent(player.PlayerId, NexusMap.NexusCoord));
            }
            else if (player.PendingBeginNexusGate && hasPlanetaryOnNexus)
            {
                if (player.GateProgress == NexusGateProgress.None)
                {
                    player.GateProgress = NexusGateProgress.Started;
                    events.Add(new NexusGateStartedEvent(player.PlayerId, NexusMap.NexusCoord));
                }
                else if (player.GateProgress == NexusGateProgress.Started)
                {
                    player.GateProgress = NexusGateProgress.Completed;
                    events.Add(new NexusGateCompletedEvent(player.PlayerId, NexusMap.NexusCoord));
                    completedIds.Add(player.PlayerId);
                }
            }
        }

        if (completedIds.Count == 2)
        {
            state.Completion = new NexusGameCompletion(NexusGameOutcome.Draw, null);
            events.Add(new NexusDrawEvent("Both players completed the Nexus Gate simultaneously."));
        }
        else if (completedIds.Count == 1)
        {
            state.Completion = new NexusGameCompletion(NexusGameOutcome.Victory, completedIds[0]);
            events.Add(new NexusVictoryEvent(completedIds[0]));
        }

        state.LastResolveEvents = events;

        if (state.Completion is null)
        {
            state.RoundNumber++;
            foreach (var player in players)
            {
                player.HasSubmittedOrders = false;
                player.PendingMoveOrders = [];
                player.PendingBuildOrders = [];
                player.PendingBeginNexusGate = false;
            }
        }
    }

    // ── Moves ─────────────────────────────────────────────────────────────────

    private static void ApplyMoves(
        NexusGameState state,
        NexusPlayerState p1,
        NexusPlayerState p2,
        List<NexusResolveEvent> events
    )
    {
        foreach (var player in new[] { p1, p2 })
        {
            foreach (var order in player.PendingMoveOrders)
            {
                var src = GetSystem(state, order.From)!;
                var dst = GetSystem(state, order.To)!;

                foreach (var (unitType, count) in order.Units)
                {
                    src.RemoveUnits(player.PlayerId, unitType, count);
                    dst.AddUnits(player.PlayerId, unitType, count);
                }

                events.Add(
                    new NexusUnitsMovedEvent(player.PlayerId, order.From, order.To, order.Units)
                );
            }
        }

        // Update control for all move destinations
        var destinations = p1
            .PendingMoveOrders.Select(o => o.To)
            .Concat(p2.PendingMoveOrders.Select(o => o.To))
            .Distinct();

        foreach (var coord in destinations)
        {
            var system = GetSystem(state, coord)!;
            UpdateSystemControl(
                system,
                p1.PlayerId,
                p2.PlayerId,
                groundCombatOccurred: false,
                events
            );
        }
    }

    // ── Combat ────────────────────────────────────────────────────────────────

    private static void ResolveCombat(
        NexusGameState state,
        Guid player1Id,
        Guid player2Id,
        List<NexusResolveEvent> events,
        Random rng
    )
    {
        foreach (var system in state.Systems)
        {
            if (!system.HasAnyUnits(player1Id) || !system.HasAnyUnits(player2Id))
                continue;

            events.Add(new NexusCombatBeganEvent(system.Coord, player1Id, player2Id));
            ResolveSystemCombat(system, player1Id, player2Id, events, rng);
        }
    }

    private static void ResolveSystemCombat(
        NexusSystemState system,
        Guid player1Id,
        Guid player2Id,
        List<NexusResolveEvent> events,
        Random rng
    )
    {
        var units1 = ExpandUnits(system.GetPlayerUnits(player1Id));
        var units2 = ExpandUnits(system.GetPlayerUnits(player2Id));

        var hadPlanetary1 = units1.Any(u => u.Type.IsPlanetary());
        var hadPlanetary2 = units2.Any(u => u.Type.IsPlanetary());
        var groundCombatOccurred = hadPlanetary1 && hadPlanetary2;

        for (var phase = 1; phase <= 4; phase++)
        {
            var canAttack1 = units1.Any(u =>
                !u.IsDestroyed && NexusCombatSpec.CanAttack(u.Type, phase)
            );
            var canAttack2 = units2.Any(u =>
                !u.IsDestroyed && NexusCombatSpec.CanAttack(u.Type, phase)
            );
            if (!canAttack1 && !canAttack2)
                continue;

            var losses = new Dictionary<(Guid, NexusUnitType), int>();
            var attackRolls = new List<NexusCombatAttackRoll>();
            RunAttacks(units1, units2, player1Id, player2Id, phase, rng, losses, attackRolls);
            RunAttacks(units2, units1, player2Id, player1Id, phase, rng, losses, attackRolls);

            events.Add(
                new NexusPhaseResultEvent(
                    system.Coord,
                    phase,
                    losses
                        .Select(kv => new NexusCombatLoss(kv.Key.Item1, kv.Key.Item2, kv.Value))
                        .ToImmutableArray(),
                    attackRolls.ToImmutableArray()
                )
            );
        }

        var survivors1 = CollapseAlive(units1);
        var survivors2 = CollapseAlive(units2);

        var p1Cleared = units1.All(u => u.IsDestroyed);
        var p2Cleared = units2.All(u => u.IsDestroyed);

        if (p2Cleared && !p1Cleared)
            events.Add(new NexusSystemClearedEvent(system.Coord, player1Id));
        else if (p1Cleared && !p2Cleared)
            events.Add(new NexusSystemClearedEvent(system.Coord, player2Id));

        // Write survivors back
        if (survivors1.Count > 0)
            system.Units[player1Id] = survivors1;
        else
            system.Units.Remove(player1Id);

        if (survivors2.Count > 0)
            system.Units[player2Id] = survivors2;
        else
            system.Units.Remove(player2Id);

        UpdateSystemControl(system, player1Id, player2Id, groundCombatOccurred, events);
    }

    private static void RunAttacks(
        List<CombatUnit> attackers,
        List<CombatUnit> targets,
        Guid attackerPlayerId,
        Guid targetPlayerId,
        int phase,
        Random rng,
        Dictionary<(Guid, NexusUnitType), int> losses,
        List<NexusCombatAttackRoll> attackRolls
    )
    {
        foreach (var attacker in attackers)
        {
            if (attacker.IsDestroyed || !NexusCombatSpec.CanAttack(attacker.Type, phase))
                continue;

            var eligible = targets
                .Where(t =>
                    !t.IsDestroyed
                    && NexusCombatSpec.IsTargetable(t.Type, phase)
                    && NexusCombatSpec.GetHitThreshold(attacker.Type, phase, t.Type) is not null
                )
                .ToList();

            if (eligible.Count == 0)
                continue;

            var target = PickTargetByWeight(eligible, rng);
            var threshold = NexusCombatSpec
                .GetHitThreshold(attacker.Type, phase, target.Type)!
                .Value;
            var roll = rng.Next(1, 7); // 1–6 inclusive
            var isHit = roll >= threshold;

            attackRolls.Add(
                new NexusCombatAttackRoll(
                    attackerPlayerId,
                    attacker.Type,
                    target.Type,
                    roll,
                    threshold,
                    isHit
                )
            );

            if (isHit)
            {
                target.HitsAbsorbed++;
                if (target.IsDestroyed)
                {
                    var key = (targetPlayerId, target.Type);
                    losses[key] = losses.GetValueOrDefault(key) + 1;
                }
            }
        }
    }

    // ── Control ───────────────────────────────────────────────────────────────

    private static void UpdateSystemControl(
        NexusSystemState system,
        Guid player1Id,
        Guid player2Id,
        bool groundCombatOccurred,
        List<NexusResolveEvent> events
    )
    {
        var p1HasPlanetary = system.HasPlanetaryUnits(player1Id);
        var p2HasPlanetary = system.HasPlanetaryUnits(player2Id);

        if (p1HasPlanetary && !p2HasPlanetary)
        {
            if (system.ControlOwner != player1Id)
            {
                system.ControlOwner = player1Id;
                events.Add(new NexusPlanetaryControlEvent(system.Coord, player1Id));
            }
        }
        else if (p2HasPlanetary && !p1HasPlanetary)
        {
            if (system.ControlOwner != player2Id)
            {
                system.ControlOwner = player2Id;
                events.Add(new NexusPlanetaryControlEvent(system.Coord, player2Id));
            }
        }
        else if (p1HasPlanetary && p2HasPlanetary)
        {
            if (system.ControlOwner is not null)
            {
                system.ControlOwner = null;
                events.Add(new NexusSystemContestedEvent(system.Coord));
            }
        }
        else if (groundCombatOccurred && system.ControlOwner is not null)
        {
            // All planetary units wiped out in ground combat
            system.ControlOwner = null;
            events.Add(new NexusSystemUncontrolledEvent(system.Coord));
        }
        // else: capital ships or strike craft only — retain existing control
    }

    // ── Combat Helpers ────────────────────────────────────────────────────────

    private sealed class CombatUnit(NexusUnitType type)
    {
        public NexusUnitType Type { get; } = type;
        public int HitsAbsorbed { get; set; }
        public bool IsDestroyed => HitsAbsorbed >= Type.Hull();
    }

    private static List<CombatUnit> ExpandUnits(Dictionary<NexusUnitType, int> units)
    {
        var result = new List<CombatUnit>();
        foreach (var (type, count) in units)
            for (var i = 0; i < count; i++)
                result.Add(new CombatUnit(type));
        return result;
    }

    private static Dictionary<NexusUnitType, int> CollapseAlive(List<CombatUnit> units)
    {
        var result = new Dictionary<NexusUnitType, int>();
        foreach (var unit in units.Where(u => !u.IsDestroyed))
            result[unit.Type] = result.GetValueOrDefault(unit.Type) + 1;
        return result;
    }

    private static CombatUnit PickTargetByWeight(List<CombatUnit> targets, Random rng)
    {
        var totalWeight = targets.Sum(t => t.Type.Silhouette());
        var pick = rng.Next(totalWeight);
        var cumulative = 0;
        foreach (var target in targets)
        {
            cumulative += target.Type.Silhouette();
            if (pick < cumulative)
                return target;
        }
        return targets[^1];
    }

    // ── View Projection ───────────────────────────────────────────────────────

    private static NexusPlayerView ProjectPlayerView(NexusPlayerState player, bool isSelf) =>
        new(
            player.PlayerId,
            player.Faction,
            player.Energy,
            player.GateProgress,
            player.HasSubmittedOrders,
            player.IsActive,
            isSelf ? player.PendingMoveOrders.ToImmutableArray() : null,
            isSelf ? player.PendingBuildOrders.ToImmutableArray() : null,
            isSelf && player.PendingBeginNexusGate
        );

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static NexusPlayerState? GetPlayer(NexusGameState state, Guid playerId) =>
        state.Players.FirstOrDefault(p => p.PlayerId == playerId);

    private static NexusSystemState? GetSystem(NexusGameState state, HexCoord coord) =>
        state.Systems.FirstOrDefault(s => s.Coord == coord);
}
