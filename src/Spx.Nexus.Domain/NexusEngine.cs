using System.Collections.Immutable;

namespace Spx.Nexus.Domain;

public static class NexusEngine
{
    private const int GateCost = 12;

    // ── Public API ────────────────────────────────────────────────────────────

    public static void Initialize(NexusState state, InitializeNexusGameCommand command, Random rng)
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
        NexusState state,
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

    public static void Abandon(NexusState state, Guid playerId)
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

    public static NexusGameView BuildView(NexusState state, Guid gameId, Guid playerId)
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
                    outer =>
                        outer
                            .Value.GroupBy(st => st.UnitType)
                            .ToImmutableDictionary(g => g.Key, g => g.Sum(st => st.Count))
                )
            ))
            .ToImmutableArray();

        return new NexusGameView(
            gameId,
            state.RoundNumber,
            systems,
            ProjectPlayerView(current, isSelf: true, state),
            ProjectPlayerView(opponent, isSelf: false, state),
            state.LastResolveEvents.ToImmutableArray(),
            state.Completion
        );
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private static NexusTurnOrdersRejected? ValidateMoveOrders(
        NexusState state,
        Guid playerId,
        IEnumerable<NexusMoveOrder> orders
    )
    {
        var committed = new Dictionary<HexCoord, Dictionary<NexusUnitType, int>>();

        foreach (var order in orders)
        {
            if (!NexusMap.IsValidCoord(order.From))
                return new NexusTurnOrdersRejected("Selected Source System is not on the map.");
            if (!NexusMap.IsValidCoord(order.To))
                return new NexusTurnOrdersRejected(
                    "Selected Destination System is not on the map."
                );
            if (!NexusMap.AreAdjacent(order.From, order.To))
                return new NexusTurnOrdersRejected(
                    $"{FormatSystem(state, playerId, order.To)} is not adjacent to {FormatSystem(state, playerId, order.From)}."
                );
            if (order.Units.Count == 0)
                return new NexusTurnOrdersRejected("A move order must include at least one unit.");

            var system = GetSystem(state, order.From);
            if (system is null)
                return new NexusTurnOrdersRejected(
                    $"Source System {FormatSystem(state, playerId, order.From)} was not found in the current game state."
                );

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
                {
                    if (unitType.CarryCapacity() > 0)
                    {
                        var availableCapacity = GetAvailableCarryCapacity(system, playerId);
                        var requestedCapacity =
                            GetCommittedCarryCapacity(fromCommitted)
                            + GetRequestedCarryCapacity(order.Units);

                        return new NexusTurnOrdersRejected(
                            $"Insufficient Fleet Capacity at {FormatSystem(state, playerId, order.From)}: "
                                + $"need {requestedCapacity}, have {availableCapacity}."
                        );
                    }

                    return new NexusTurnOrdersRejected(
                        $"Insufficient {unitType} at {FormatSystem(state, playerId, order.From)}: "
                            + $"need {alreadyCommitted + count}, have {available}."
                    );
                }

                fromCommitted[unitType] = alreadyCommitted + count;
                capacityProvided += unitType.CarryCapacity() * count;
                capacityNeeded += unitType.ConsumedCapacity() * count;
            }

            if (capacityNeeded > capacityProvided)
                return new NexusTurnOrdersRejected(
                    $"Insufficient Fleet Capacity for move from {FormatSystem(state, playerId, order.From)} to {FormatSystem(state, playerId, order.To)}: "
                        + $"need {capacityNeeded}, have {capacityProvided}."
                );
        }

        return null;
    }

    private static int GetAvailableCarryCapacity(NexusSystemState system, Guid playerId) =>
        system.GetPlayerUnits(playerId).Sum(kv => kv.Key.CarryCapacity() * kv.Value);

    private static int GetCommittedCarryCapacity(Dictionary<NexusUnitType, int> committedUnits) =>
        committedUnits.Sum(kv => kv.Key.CarryCapacity() * kv.Value);

    private static int GetRequestedCarryCapacity(
        IReadOnlyDictionary<NexusUnitType, int> requestedUnits
    ) => requestedUnits.Sum(kv => kv.Key.CarryCapacity() * kv.Value);

    private static string FormatSystem(NexusState state, Guid playerId, HexCoord coord)
    {
        var homePlayerId = GetSystem(state, coord)?.HomePlayerId;
        if (homePlayerId == playerId)
            return "Your Home System";

        if (homePlayerId.HasValue)
            return "Opponent Home System";

        return NexusMap.GetSectorDisplayName(coord);
    }

    private static NexusTurnOrdersRejected? ValidateBuildAndGate(
        NexusState state,
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

    private static void Resolve(NexusState state, Random rng)
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

        // Step 5b: Supply check — disband Capitals over supply pool in spiral order
        ResolveSupplyCheck(state, players, events);

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

    // ── Supply Check ──────────────────────────────────────────────────────────

    private static void ResolveSupplyCheck(
        NexusState state,
        List<NexusPlayerState> players,
        List<NexusResolveEvent> events
    )
    {
        foreach (var player in players)
        {
            var supplyPool = ComputeSupplyPool(state, player.PlayerId);
            var capitalCount = ComputeCapitalCount(state, player.PlayerId);
            var deficit = capitalCount - supplyPool;

            if (deficit <= 0)
                continue;

            foreach (var coord in NexusMap.SystemsInSpiralOrder)
            {
                if (deficit <= 0)
                    break;

                var system = GetSystem(state, coord);
                if (system is null)
                    continue;

                // Collect Capital stacks for this player, cheapest type first.
                // All Capital costs are unique (4/5/6/8) so ordering by cost is deterministic.
                // Within a type, most-damaged stacks are taken first.
                var capitalStacks = system
                    .GetPlayerStacks(player.PlayerId)
                    .Where(s => s.UnitType.IsCapital())
                    .OrderBy(s => s.UnitType.Cost())
                    .ThenByDescending(s => s.HitsAbsorbed)
                    .ToList();

                if (capitalStacks.Count == 0)
                    continue;

                NexusUnitType? currentType = null;
                var countForCurrentType = 0;

                foreach (var stack in capitalStacks)
                {
                    if (deficit <= 0)
                        break;

                    if (currentType.HasValue && currentType.Value != stack.UnitType)
                    {
                        // Emit batched event for the previous type
                        events.Add(
                            new NexusCapitalDisbandedEvent(
                                player.PlayerId,
                                currentType.Value,
                                coord,
                                countForCurrentType
                            )
                        );
                        countForCurrentType = 0;
                    }

                    currentType = stack.UnitType;
                    var take = Math.Min(stack.Count, deficit);
                    system.RemoveUnits(player.PlayerId, stack.UnitType, take);
                    deficit -= take;
                    countForCurrentType += take;
                }

                if (currentType.HasValue && countForCurrentType > 0)
                {
                    events.Add(
                        new NexusCapitalDisbandedEvent(
                            player.PlayerId,
                            currentType.Value,
                            coord,
                            countForCurrentType
                        )
                    );
                }
            }
        }
    }

    private static int ComputeSupplyPool(NexusState state, Guid playerId) =>
        state.Systems.Where(s => s.ControlOwner == playerId).Sum(s => s.IncomeValue);

    private static int ComputeCapitalCount(NexusState state, Guid playerId) =>
        state.Systems.Sum(s =>
            s.GetUnitCount(playerId, NexusUnitType.Frigate)
            + s.GetUnitCount(playerId, NexusUnitType.Destroyer)
            + s.GetUnitCount(playerId, NexusUnitType.Cruiser)
            + s.GetUnitCount(playerId, NexusUnitType.Carrier)
        );

    // ── Moves ─────────────────────────────────────────────────────────────────

    private static void ApplyMoves(
        NexusState state,
        NexusPlayerState p1,
        NexusPlayerState p2,
        List<NexusResolveEvent> events
    )
    {
        var contestedSystems = state
            .Systems.Where(s => s.HasAnyUnits(p1.PlayerId) && s.HasAnyUnits(p2.PlayerId))
            .Select(s => s.Coord)
            .ToHashSet();

        foreach (var player in new[] { p1, p2 })
        {
            foreach (var order in player.PendingMoveOrders)
            {
                var src = GetSystem(state, order.From)!;
                var dst = GetSystem(state, order.To)!;
                var retreating = contestedSystems.Contains(order.From);

                foreach (var (unitType, count) in order.Units)
                {
                    var taken = src.TakeUnits(player.PlayerId, unitType, count, retreating);
                    foreach (var (hitsAbsorbed, takenCount) in taken)
                        dst.AddUnits(player.PlayerId, unitType, takenCount, hitsAbsorbed);
                }

                events.Add(
                    new NexusUnitsMovedEvent(
                        player.PlayerId,
                        order.From,
                        order.To,
                        order.Units,
                        retreating
                    )
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
        NexusState state,
        Guid player1Id,
        Guid player2Id,
        List<NexusResolveEvent> events,
        Random rng
    )
    {
        foreach (var coord in NexusMap.SystemsInSpiralOrder)
        {
            var system = GetSystem(state, coord);
            if (system is null || !system.HasAnyUnits(player1Id) || !system.HasAnyUnits(player2Id))
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
        var units1 = ExpandUnits(system.GetPlayerStacks(player1Id));
        var units2 = ExpandUnits(system.GetPlayerStacks(player2Id));

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

            var pendingHits = new List<(CombatUnit Target, Guid TargetPlayerId)>();
            var attackRolls = new List<NexusCombatAttackRoll>();
            GatherAttacks(
                units1,
                units2,
                player1Id,
                player2Id,
                phase,
                rng,
                pendingHits,
                attackRolls
            );
            GatherAttacks(
                units2,
                units1,
                player2Id,
                player1Id,
                phase,
                rng,
                pendingHits,
                attackRolls
            );

            var losses = new Dictionary<(Guid, NexusUnitType), int>();
            foreach (var (target, targetPlayerId) in pendingHits)
            {
                target.HitsAbsorbed++;
                if (target.IsDestroyed)
                {
                    var key = (targetPlayerId, target.Type);
                    losses[key] = losses.GetValueOrDefault(key) + 1;
                }
            }

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

    private static void GatherAttacks(
        List<CombatUnit> attackers,
        List<CombatUnit> targets,
        Guid attackerPlayerId,
        Guid targetPlayerId,
        int phase,
        Random rng,
        List<(CombatUnit Target, Guid TargetPlayerId)> pendingHits,
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
                pendingHits.Add((target, targetPlayerId));
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

    private static List<CombatUnit> ExpandUnits(IReadOnlyList<NexusUnitStack> stacks)
    {
        var result = new List<CombatUnit>();
        foreach (var stack in stacks)
            for (var i = 0; i < stack.Count; i++)
                result.Add(new CombatUnit(stack.UnitType) { HitsAbsorbed = stack.HitsAbsorbed });
        return result;
    }

    private static List<NexusUnitStack> CollapseAlive(List<CombatUnit> units)
    {
        var grouped = new Dictionary<(NexusUnitType, int), int>();
        foreach (var unit in units.Where(u => !u.IsDestroyed))
        {
            var key = (unit.Type, unit.HitsAbsorbed);
            grouped[key] = grouped.GetValueOrDefault(key) + 1;
        }
        return grouped
            .Select(kv => new NexusUnitStack
            {
                UnitType = kv.Key.Item1,
                HitsAbsorbed = kv.Key.Item2,
                Count = kv.Value,
            })
            .ToList();
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

    private static NexusPlayerView ProjectPlayerView(
        NexusPlayerState player,
        bool isSelf,
        NexusState state
    ) =>
        new(
            player.PlayerId,
            player.Faction,
            player.Energy,
            player.GateProgress,
            player.HasSubmittedOrders,
            player.IsActive,
            isSelf ? player.PendingMoveOrders.ToImmutableArray() : null,
            isSelf ? player.PendingBuildOrders.ToImmutableArray() : null,
            isSelf && player.PendingBeginNexusGate,
            ComputeSupplyPool(state, player.PlayerId),
            ComputeCapitalCount(state, player.PlayerId)
        );

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static NexusPlayerState? GetPlayer(NexusState state, Guid playerId) =>
        state.Players.FirstOrDefault(p => p.PlayerId == playerId);

    private static NexusSystemState? GetSystem(NexusState state, HexCoord coord) =>
        state.Systems.FirstOrDefault(s => s.Coord == coord);
}
