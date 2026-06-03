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
                ProjectUnitStacks(state, s),
                IsSystemContested(s, playerId, opponent.PlayerId)
                    ? ProjectUnitStacks(state, s, excludePlanetary: true)
                    : null
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
        var opponentId = state.Players.First(p => p.PlayerId != playerId).PlayerId;
        var committed =
            new Dictionary<
                HexCoord,
                Dictionary<(NexusUnitType UnitType, int RemainingHits), int>
            >();

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
            if (order.Stacks.Length == 0)
                return new NexusTurnOrdersRejected("A move order must include at least one unit.");

            var system = GetSystem(state, order.From);
            if (system is null)
                return new NexusTurnOrdersRejected(
                    $"Source System {FormatSystem(state, playerId, order.From)} was not found in the current game state."
                );

            if (IsSystemContested(system, playerId, opponentId))
                foreach (var stack in order.Stacks)
                    if (stack.UnitType.IsPlanetary())
                        return new NexusTurnOrdersRejected(
                            $"Planetary units cannot move from a contested system ({FormatSystem(state, playerId, order.From)})."
                        );

            if (!committed.TryGetValue(order.From, out var fromCommitted))
            {
                fromCommitted = [];
                committed[order.From] = fromCommitted;
            }

            var capacityProvided = 0;
            var capacityNeeded = 0;

            foreach (var stack in order.Stacks)
            {
                if (stack.Count <= 0)
                    return new NexusTurnOrdersRejected(
                        $"Unit count for {stack.UnitType} must be positive."
                    );

                if (stack.RemainingHits <= 0 || stack.RemainingHits > stack.UnitType.Profile().Hits)
                    return new NexusTurnOrdersRejected(
                        $"Remaining hits for {stack.UnitType} must be between 1 and {stack.UnitType.Profile().Hits}."
                    );

                var key = (stack.UnitType, stack.RemainingHits);
                var alreadyCommitted = fromCommitted.GetValueOrDefault(key);
                var available = system
                    .GetPlayerStacks(playerId)
                    .Where(s =>
                        s.UnitType == stack.UnitType && s.RemainingHits == stack.RemainingHits
                    )
                    .Sum(s => s.Count);

                if (alreadyCommitted + stack.Count > available)
                {
                    if (stack.UnitType.CarryCapacity() > 0)
                    {
                        var availableCapacity = GetAvailableCarryCapacity(system, playerId);
                        var requestedCapacity =
                            GetCommittedCarryCapacity(fromCommitted)
                            + GetRequestedCarryCapacity(order.Stacks);

                        return new NexusTurnOrdersRejected(
                            $"Insufficient Fleet Capacity at {FormatSystem(state, playerId, order.From)}: "
                                + $"need {requestedCapacity}, have {availableCapacity}."
                        );
                    }

                    return new NexusTurnOrdersRejected(
                        $"Insufficient {FormatStack(stack)} at {FormatSystem(state, playerId, order.From)}: "
                            + $"need {alreadyCommitted + stack.Count}, have {available}."
                    );
                }

                fromCommitted[key] = alreadyCommitted + stack.Count;
                capacityProvided += stack.UnitType.CarryCapacity() * stack.Count;
                capacityNeeded += stack.UnitType.ConsumedCapacity() * stack.Count;
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

    private static int GetCommittedCarryCapacity(
        Dictionary<(NexusUnitType UnitType, int RemainingHits), int> committedUnits
    ) => committedUnits.Sum(kv => kv.Key.UnitType.CarryCapacity() * kv.Value);

    private static int GetRequestedCarryCapacity(
        ImmutableArray<NexusUnitStackGroup> requestedStacks
    ) => requestedStacks.Sum(stack => stack.UnitType.CarryCapacity() * stack.Count);

    private static string FormatStack(NexusUnitStackGroup stack) =>
        stack.RemainingHits == stack.UnitType.Profile().Hits
            ? stack.UnitType.ToString()
            : $"{stack.UnitType} ({stack.RemainingHits}/{stack.UnitType.Profile().Hits} hits)";

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

            var opponentId = state.Players.First(p => p.PlayerId != player.PlayerId).PlayerId;
            if (nexusSystem.HasAnyUnits(opponentId))
                return new NexusTurnOrdersRejected(
                    "Cannot begin Nexus Gate: the Nexus is contested."
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
        ResolveGateProgressAndWinCheck(state, players, player1.PlayerId, player2.PlayerId, events);

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
                // Within a type, lowest remaining hits is taken first.
                var capitalStacks = system
                    .GetPlayerStacks(player.PlayerId)
                    .Where(s => s.UnitType.IsCapital())
                    .OrderBy(s => s.UnitType.Cost())
                    .ThenBy(s => s.RemainingHits)
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

    private static void ResolveGateProgressAndWinCheck(
        NexusState state,
        List<NexusPlayerState> players,
        Guid player1Id,
        Guid player2Id,
        List<NexusResolveEvent> events
    )
    {
        var completedIds = new List<Guid>();
        foreach (var player in players)
        {
            var nexus = GetSystem(state, NexusMap.NexusCoord);
            var hasPlanetaryOnNexus = nexus is not null && nexus.HasPlanetaryUnits(player.PlayerId);
            var nexusIsContested =
                nexus is not null && IsSystemContested(nexus, player1Id, player2Id);

            if (player.PendingBeginNexusGate && (!hasPlanetaryOnNexus || nexusIsContested))
            {
                player.Energy += GateCost;
                if (player.GateProgress == NexusGateProgress.Started)
                    player.GateProgress = NexusGateProgress.None;

                events.Add(new NexusGateCancelledEvent(player.PlayerId, NexusMap.NexusCoord));
                continue;
            }

            if (player.GateProgress == NexusGateProgress.Started && !player.PendingBeginNexusGate)
            {
                player.GateProgress = NexusGateProgress.None;
                events.Add(new NexusGateCancelledEvent(player.PlayerId, NexusMap.NexusCoord));
                continue;
            }

            if (!player.PendingBeginNexusGate || !hasPlanetaryOnNexus || nexusIsContested)
                continue;

            if (player.GateProgress == NexusGateProgress.None)
            {
                player.GateProgress = NexusGateProgress.Started;
                events.Add(new NexusGateStartedEvent(player.PlayerId, NexusMap.NexusCoord));
                continue;
            }

            if (player.GateProgress != NexusGateProgress.Started)
                continue;

            player.GateProgress = NexusGateProgress.Completed;
            events.Add(new NexusGateCompletedEvent(player.PlayerId, NexusMap.NexusCoord));
            completedIds.Add(player.PlayerId);
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
    }

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
                var movedStacks = src.TakeExactUnits(player.PlayerId, order.Stacks);
                foreach (var movedStack in movedStacks)
                    dst.AddUnits(
                        player.PlayerId,
                        movedStack.UnitType,
                        movedStack.Count,
                        movedStack.RemainingHits
                    );

                events.Add(
                    new NexusUnitsMovedEvent(
                        player.PlayerId,
                        order.From,
                        order.To,
                        movedStacks,
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

        // First strike step — units with FirstStrike tag fire first
        ResolveOneStep(
            system.Coord,
            units1,
            units2,
            player1Id,
            player2Id,
            events,
            rng,
            isFirstStrike: true
        );

        // Normal step — surviving units without FirstStrike fire
        ResolveOneStep(
            system.Coord,
            units1,
            units2,
            player1Id,
            player2Id,
            events,
            rng,
            isFirstStrike: false
        );

        var surviving1 = CollapseAlive(units1);
        var surviving2 = CollapseAlive(units2);

        var p1Cleared = units1.All(u => u.IsDestroyed);
        var p2Cleared = units2.All(u => u.IsDestroyed);

        if (p2Cleared && !p1Cleared)
            events.Add(new NexusSystemClearedEvent(system.Coord, player1Id));
        else if (p1Cleared && !p2Cleared)
            events.Add(new NexusSystemClearedEvent(system.Coord, player2Id));

        // Write survivors back
        if (surviving1.Count > 0)
            system.Units[player1Id] = surviving1;
        else
            system.Units.Remove(player1Id);

        if (surviving2.Count > 0)
            system.Units[player2Id] = surviving2;
        else
            system.Units.Remove(player2Id);

        UpdateSystemControl(system, player1Id, player2Id, groundCombatOccurred, events);
    }

    private static void ResolveOneStep(
        HexCoord systemCoord,
        List<CombatUnit> units1,
        List<CombatUnit> units2,
        Guid player1Id,
        Guid player2Id,
        List<NexusResolveEvent> events,
        Random rng,
        bool isFirstStrike
    )
    {
        // Filter to attackers eligible for this step
        var eligible1 = units1
            .Where(u =>
                !u.IsDestroyed
                && u.Type.Profile().Tags.HasFlag(NexusUnitTag.FirstStrike) == isFirstStrike
            )
            .ToList();
        var eligible2 = units2
            .Where(u =>
                !u.IsDestroyed
                && u.Type.Profile().Tags.HasFlag(NexusUnitTag.FirstStrike) == isFirstStrike
            )
            .ToList();

        if (eligible1.Count == 0 && eligible2.Count == 0)
            return;

        var pendingHits = new List<(CombatUnit Target, Guid TargetPlayerId, bool WasShielded)>();
        var attackRolls = new List<NexusCombatAttackRoll>();
        GatherAttacks(eligible1, units2, player1Id, player2Id, rng, pendingHits, attackRolls);
        GatherAttacks(eligible2, units1, player2Id, player1Id, rng, pendingHits, attackRolls);

        var losses = new Dictionary<(Guid, NexusUnitType), int>();
        foreach (var (target, targetPlayerId, wasShielded) in pendingHits)
        {
            if (!wasShielded)
                target.RemainingHits--;
            if (target.IsDestroyed)
            {
                var key = (targetPlayerId, target.Type);
                losses[key] = losses.GetValueOrDefault(key) + 1;
            }
        }

        if (isFirstStrike)
        {
            events.Add(
                new NexusFirstStrikeEvent(
                    systemCoord,
                    losses
                        .Select(kv => new NexusCombatLoss(kv.Key.Item1, kv.Key.Item2, kv.Value))
                        .ToImmutableArray(),
                    attackRolls.ToImmutableArray()
                )
            );
        }
        else
        {
            events.Add(
                new NexusCombatResultEvent(
                    systemCoord,
                    losses
                        .Select(kv => new NexusCombatLoss(kv.Key.Item1, kv.Key.Item2, kv.Value))
                        .ToImmutableArray(),
                    attackRolls.ToImmutableArray()
                )
            );
        }
    }

    private static void GatherAttacks(
        List<CombatUnit> attackers,
        List<CombatUnit> targets,
        Guid attackerPlayerId,
        Guid targetPlayerId,
        Random rng,
        List<(CombatUnit Target, Guid TargetPlayerId, bool WasShielded)> pendingHits,
        List<NexusCombatAttackRoll> attackRolls
    )
    {
        foreach (var attacker in attackers)
        {
            if (attacker.IsDestroyed)
                continue;

            var eligible = targets
                .Where(t =>
                    !t.IsDestroyed
                    && NexusCombatSpec.GetHitThreshold(attacker.Type, t.Type) is not null
                )
                .ToList();

            if (eligible.Count == 0)
                continue;

            var profile = attacker.Type.Profile();
            var tags = profile.Tags;

            // Local function to perform one attack roll against a given pool of eligible targets
            void PerformAttack(List<CombatUnit> pool)
            {
                if (pool.Count == 0)
                    return;

                var target = PickTargetByWeight(pool, rng);
                var threshold = NexusCombatSpec.GetHitThreshold(attacker.Type, target.Type)!.Value;
                var roll = rng.Next(1, 7); // 1–6 inclusive
                var isHit = roll >= threshold;
                var wasShielded = false;
                if (isHit && target.ShieldActive && !tags.HasFlag(NexusUnitTag.IgnoreShield))
                {
                    // Shield absorbs on 4+; if it fails the hit passes through and the shield is NOT consumed.
                    var shieldRoll = rng.Next(1, 7);
                    if (shieldRoll >= 4)
                    {
                        wasShielded = true;
                        target.ShieldActive = false;
                    }
                }

                attackRolls.Add(
                    new NexusCombatAttackRoll(
                        attackerPlayerId,
                        attacker.Type,
                        target.Type,
                        roll,
                        threshold,
                        isHit,
                        attacker.RemainingHits,
                        targetPlayerId,
                        target.RemainingHits,
                        wasShielded
                    )
                );

                if (isHit)
                    pendingHits.Add((target, targetPlayerId, wasShielded));
            }

            // Normal attacks
            for (var i = 0; i < profile.Attacks; i++)
                PerformAttack(eligible);

            // Free extra attacks restricted to a specific category
            if (tags.HasFlag(NexusUnitTag.FreeAttackVsStrike))
                PerformAttack(eligible.Where(t => t.Type.IsStrike()).ToList());
            if (tags.HasFlag(NexusUnitTag.FreeAttackVsCapital))
                PerformAttack(eligible.Where(t => t.Type.IsCapital()).ToList());
            if (tags.HasFlag(NexusUnitTag.FreeAttackVsPlanetary))
                PerformAttack(eligible.Where(t => t.Type.IsPlanetary()).ToList());
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
        if (system.IsNexus)
        {
            system.ControlOwner = null;
            return;
        }

        var p1HasPresence = system.HasAnyUnits(player1Id);
        var p2HasPresence = system.HasAnyUnits(player2Id);
        var p1HasPlanetary = system.HasPlanetaryUnits(player1Id);
        var p2HasPlanetary = system.HasPlanetaryUnits(player2Id);

        if (p1HasPresence && p2HasPresence)
        {
            if (system.ControlOwner is not null)
            {
                system.ControlOwner = null;
                events.Add(new NexusSystemContestedEvent(system.Coord));
            }
        }
        else if (p1HasPlanetary && !p2HasPresence)
        {
            if (system.ControlOwner != player1Id)
            {
                system.ControlOwner = player1Id;
                events.Add(new NexusPlanetaryControlEvent(system.Coord, player1Id));
            }
        }
        else if (p2HasPlanetary && !p1HasPresence)
        {
            if (system.ControlOwner != player2Id)
            {
                system.ControlOwner = player2Id;
                events.Add(new NexusPlanetaryControlEvent(system.Coord, player2Id));
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
        public int RemainingHits { get; set; } = type.Profile().Hits;
        public bool ShieldActive { get; set; } = type.Profile().Tags.HasFlag(NexusUnitTag.Shield);
        public bool IsDestroyed => RemainingHits <= 0;
    }

    private static List<CombatUnit> ExpandUnits(IReadOnlyList<NexusUnitStack> stacks)
    {
        var result = new List<CombatUnit>();
        foreach (var stack in stacks)
            for (var i = 0; i < stack.Count; i++)
                result.Add(new CombatUnit(stack.UnitType) { RemainingHits = stack.RemainingHits });
        return result;
    }

    private static List<NexusUnitStack> CollapseAlive(List<CombatUnit> units)
    {
        var grouped = new Dictionary<(NexusUnitType, int), int>();
        foreach (var unit in units.Where(u => !u.IsDestroyed))
        {
            var key = (unit.Type, unit.RemainingHits);
            grouped[key] = grouped.GetValueOrDefault(key) + 1;
        }
        return grouped
            .Select(kv => new NexusUnitStack
            {
                UnitType = kv.Key.Item1,
                RemainingHits = kv.Key.Item2,
                Count = kv.Value,
            })
            .ToList();
    }

    private static CombatUnit PickTargetByWeight(List<CombatUnit> targets, Random rng)
    {
        // Check if the defender (targets side) has any Escort units
        var defenderHasEscort = targets.Any(t =>
            t.Type.Profile().Tags.HasFlag(NexusUnitTag.Escort)
        );

        var weights = new int[targets.Count];
        var totalWeight = 0;
        for (var i = 0; i < targets.Count; i++)
        {
            var sil = targets[i].Type.Profile().Silhouette;
            // Escort: reduce effective silhouette of non-Escort Capital units on the defender's side
            if (
                defenderHasEscort
                && targets[i].Type.IsCapital()
                && !targets[i].Type.Profile().Tags.HasFlag(NexusUnitTag.Escort)
            )
                sil = Math.Max(1, sil - 1);
            weights[i] = sil;
            totalWeight += sil;
        }

        var pick = rng.Next(totalWeight);
        var cumulative = 0;
        for (var i = 0; i < targets.Count; i++)
        {
            cumulative += weights[i];
            if (pick < cumulative)
                return targets[i];
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

    private static ImmutableDictionary<Guid, ImmutableArray<NexusUnitStackGroup>> ProjectUnitStacks(
        NexusState state,
        NexusSystemState system,
        bool excludePlanetary = false
    )
    {
        var playerIds = state.Players.Select(player => player.PlayerId).Distinct();
        var builder = ImmutableDictionary.CreateBuilder<
            Guid,
            ImmutableArray<NexusUnitStackGroup>
        >();

        foreach (var playerId in playerIds)
        {
            var stacks = system.GetPlayerStacks(playerId);
            if (stacks.Count == 0)
                continue;

            var filtered = excludePlanetary ? stacks.Where(s => !s.UnitType.IsPlanetary()) : stacks;

            var projected = filtered
                .Select(stack => new NexusUnitStackGroup(
                    stack.UnitType,
                    stack.RemainingHits,
                    stack.Count
                ))
                .OrderBy(stack => stack.UnitType)
                .ThenByDescending(stack => stack.RemainingHits)
                .ToImmutableArray();

            if (projected.Length > 0)
                builder[playerId] = projected;
        }

        return builder.ToImmutable();
    }

    private static bool IsSystemContested(
        NexusSystemState system,
        Guid player1Id,
        Guid player2Id
    ) => system.HasAnyUnits(player1Id) && system.HasAnyUnits(player2Id);

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static NexusPlayerState? GetPlayer(NexusState state, Guid playerId) =>
        state.Players.FirstOrDefault(p => p.PlayerId == playerId);

    private static NexusSystemState? GetSystem(NexusState state, HexCoord coord) =>
        state.Systems.FirstOrDefault(s => s.Coord == coord);
}
