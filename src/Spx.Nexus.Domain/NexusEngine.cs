using System.Collections.Immutable;

namespace Spx.Nexus.Domain;

public static class NexusEngine
{
    /// <summary>Energy cost to begin or continue building the Nexus Gate each round.</summary>
    public const int GateCost = 12;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Computes the total energy cost of a set of build orders and an optional
    /// Nexus Gate activation. Uses domain cost constants so callers don't
    /// duplicate magic numbers or summation logic.
    /// </summary>
    public static int ComputeProjectedSpend(
        IEnumerable<NexusBuildOrder> buildOrders,
        bool beginNexusGate,
        Dictionary<Guid, NexusUnitDesign> designs
    ) =>
        buildOrders.Sum(o =>
            designs.TryGetValue(o.DesignId, out var d)
                ? NexusHullBaselines.GetProfile(d).Cost * o.Count
                : 0
        ) + (beginNexusGate ? GateCost : 0);

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
                        Energy = 5,
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

        var designs = BuildDesignLookup(state);

        var error =
            ValidateMoveOrders(state, command.PlayerId, command.MoveOrders, designs)
            ?? ValidateBuildAndGate(state, player, command, designs);
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

        var viewDesigns = BuildDesignLookup(state);
        var systems = state
            .Systems.Select(s => new NexusSystemView(
                s.Coord,
                s.IsNexus,
                s.IncomeValue,
                s.HomePlayerId,
                s.ControlOwner,
                ProjectUnitStacks(state, s, viewDesigns),
                IsSystemContested(s, playerId, opponent.PlayerId)
                    ? ProjectUnitStacks(state, s, viewDesigns, excludePlanetary: true)
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

    // ── Design Management ─────────────────────────────────────────────────────

    public static NexusDesignCommandResult CreateDesign(
        NexusState state,
        NexusCreateDesignCommand command
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        var player = GetPlayer(state, command.PlayerId);
        if (player is null)
            return new NexusDesignCommandRejected("Player not found.");

        var tags = command.Modules.ToList();
        var validationError = NexusDesignConstraints.Validate(command.Hull, tags);
        if (validationError is not null)
            return new NexusDesignCommandRejected(validationError);

        var design = new NexusUnitDesign
        {
            DesignId = Guid.NewGuid(),
            Name = command.Name.Trim(),
            Hull = command.Hull,
            Modules = tags,
        };

        player.Designs.Add(design);
        return new NexusDesignCreated(design);
    }

    public static NexusDesignCommandResult DeleteDesign(
        NexusState state,
        NexusDeleteDesignCommand command
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        var player = GetPlayer(state, command.PlayerId);
        if (player is null)
            return new NexusDesignCommandRejected("Player not found.");

        var design = player.Designs.FirstOrDefault(d => d.DesignId == command.DesignId);
        if (design is null)
            return new NexusDesignCommandRejected("Design not found.");

        // Cannot delete a design while units of that design are on the map.
        var unitsExist = state.Systems.Any(s =>
            s.Units.Values.Any(stacks => stacks.Any(st => st.DesignId == command.DesignId))
        );
        if (unitsExist)
            return new NexusDesignCommandRejected(
                "Cannot delete a design while units of that design are on the map."
            );

        player.Designs.Remove(design);
        return new NexusDesignDeleted();
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private static NexusTurnOrdersRejected? ValidateMoveOrders(
        NexusState state,
        Guid playerId,
        IEnumerable<NexusMoveOrder> orders,
        Dictionary<Guid, NexusUnitDesign> designs
    )
    {
        var opponentId = state.Players.First(p => p.PlayerId != playerId).PlayerId;
        var committed =
            new Dictionary<HexCoord, Dictionary<(Guid DesignId, int RemainingHits), int>>();

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
                    if (stack.Category == NexusUnitCategory.Planetary)
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
                        $"Unit count for design {stack.DesignId} must be positive."
                    );

                if (!designs.TryGetValue(stack.DesignId, out var design))
                    return new NexusTurnOrdersRejected(
                        $"Unknown design {stack.DesignId} in move order."
                    );

                var profile = NexusHullBaselines.GetProfile(design);

                if (stack.RemainingHits <= 0 || stack.RemainingHits > profile.Hits)
                    return new NexusTurnOrdersRejected(
                        $"Remaining hits for {design.Name} must be between 1 and {profile.Hits}."
                    );

                var key = (stack.DesignId, stack.RemainingHits);
                var alreadyCommitted = fromCommitted.GetValueOrDefault(key);
                var available = system
                    .GetPlayerStacks(playerId)
                    .Where(s =>
                        s.DesignId == stack.DesignId && s.RemainingHits == stack.RemainingHits
                    )
                    .Sum(s => s.Count);

                if (alreadyCommitted + stack.Count > available)
                {
                    var carryCapacity = profile.CarryCapacity;
                    if (carryCapacity > 0)
                    {
                        var availableCapacity = GetAvailableCarryCapacity(
                            system,
                            playerId,
                            designs
                        );
                        var requestedCapacity =
                            GetCommittedCarryCapacity(fromCommitted, designs)
                            + GetRequestedCarryCapacity(order.Stacks, designs);

                        return new NexusTurnOrdersRejected(
                            $"Insufficient Fleet Capacity at {FormatSystem(state, playerId, order.From)}: "
                                + $"need {requestedCapacity}, have {availableCapacity}."
                        );
                    }

                    return new NexusTurnOrdersRejected(
                        $"Insufficient {FormatStack(stack, designs)} at {FormatSystem(state, playerId, order.From)}: "
                            + $"need {alreadyCommitted + stack.Count}, have {available}."
                    );
                }

                fromCommitted[key] = alreadyCommitted + stack.Count;
                capacityProvided += profile.CarryCapacity * stack.Count;
                capacityNeeded += profile.RequiresCarrier ? stack.Count : 0;
            }

            if (capacityNeeded > capacityProvided)
                return new NexusTurnOrdersRejected(
                    $"Insufficient Fleet Capacity for move from {FormatSystem(state, playerId, order.From)} to {FormatSystem(state, playerId, order.To)}: "
                        + $"need {capacityNeeded}, have {capacityProvided}."
                );
        }

        return null;
    }

    private static int GetAvailableCarryCapacity(
        NexusSystemState system,
        Guid playerId,
        Dictionary<Guid, NexusUnitDesign> designs
    ) =>
        system
            .GetPlayerStacks(playerId)
            .Sum(s =>
                designs.TryGetValue(s.DesignId, out var d)
                    ? NexusHullBaselines.GetProfile(d).CarryCapacity * s.Count
                    : 0
            );

    private static int GetCommittedCarryCapacity(
        Dictionary<(Guid DesignId, int RemainingHits), int> committedUnits,
        Dictionary<Guid, NexusUnitDesign> designs
    ) =>
        committedUnits.Sum(kv =>
            designs.TryGetValue(kv.Key.DesignId, out var d)
                ? NexusHullBaselines.GetProfile(d).CarryCapacity * kv.Value
                : 0
        );

    private static int GetRequestedCarryCapacity(
        ImmutableArray<NexusUnitStackGroup> requestedStacks,
        Dictionary<Guid, NexusUnitDesign> designs
    ) =>
        requestedStacks.Sum(stack =>
            designs.TryGetValue(stack.DesignId, out var d)
                ? NexusHullBaselines.GetProfile(d).CarryCapacity * stack.Count
                : 0
        );

    private static string FormatStack(
        NexusUnitStackGroup stack,
        Dictionary<Guid, NexusUnitDesign> designs
    )
    {
        var name = designs.TryGetValue(stack.DesignId, out var d)
            ? d.Name
            : stack.DesignId.ToString();
        var maxHits = designs.TryGetValue(stack.DesignId, out var d2)
            ? NexusHullBaselines.GetProfile(d2).Hits
            : stack.RemainingHits;
        return stack.RemainingHits == maxHits
            ? name
            : $"{name} ({stack.RemainingHits}/{maxHits} hits)";
    }

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
        NexusTurnOrdersCommand command,
        Dictionary<Guid, NexusUnitDesign> designs
    )
    {
        var buildCost = 0;
        foreach (var order in command.BuildOrders)
        {
            if (!designs.TryGetValue(order.DesignId, out var design))
                return new NexusTurnOrdersRejected(
                    $"Unknown design {order.DesignId} in build order."
                );

            // Ensure the design belongs to this player
            if (!player.Designs.Any(d => d.DesignId == order.DesignId))
                return new NexusTurnOrdersRejected(
                    $"Design '{design.Name}' does not belong to this player."
                );

            buildCost += NexusHullBaselines.GetProfile(design).Cost * order.Count;
        }

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
        var designs = BuildDesignLookup(state);

        // Step 1: Deduct build costs and gate payments
        foreach (var player in players)
        {
            var buildCost = player.PendingBuildOrders.Sum(o =>
                designs.TryGetValue(o.DesignId, out var d)
                    ? NexusHullBaselines.GetProfile(d).Cost * o.Count
                    : 0
            );
            var gateCost = player.PendingBeginNexusGate ? GateCost : 0;
            player.Energy -= buildCost + gateCost;
        }

        // Step 2: Apply moves
        ApplyMoves(state, player1, player2, events, designs);

        // Step 3: Combat
        ResolveCombat(state, player1.PlayerId, player2.PlayerId, events, rng, designs);

        // Step 3b: Repair — restore one lost hit on units with the Repair module
        foreach (var system in state.Systems)
        {
            foreach (var (_, stacks) in system.Units)
            {
                foreach (var stack in stacks)
                {
                    if (!designs.TryGetValue(stack.DesignId, out var design))
                        continue;
                    if (!design.Modules.OfType<Repair>().Any())
                        continue;
                    var maxHits = NexusHullBaselines.GetProfile(design).Hits;
                    if (stack.RemainingHits < maxHits)
                        stack.RemainingHits = Math.Min(stack.RemainingHits + 1, maxHits);
                }
            }
        }

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
                if (!designs.TryGetValue(order.DesignId, out var design))
                    continue;

                var profile = NexusHullBaselines.GetProfile(design);
                home.AddUnits(
                    player.PlayerId,
                    design.DesignId,
                    design.Hull,
                    order.Count,
                    designHits: profile.Hits
                );
                events.Add(
                    new NexusUnitDeployedEvent(
                        player.PlayerId,
                        design.DesignId,
                        design.Name,
                        home.Coord,
                        order.Count
                    )
                );
            }
        }

        // Step 5b: Supply check — disband Capitals over supply pool in spiral order
        ResolveSupplyCheck(state, players, events, designs);

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
        List<NexusResolveEvent> events,
        Dictionary<Guid, NexusUnitDesign> designs
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

                // Collect Capital stacks for this player, cheapest design cost first.
                // Within a design, lowest remaining hits is taken first.
                var capitalStacks = system
                    .GetPlayerStacks(player.PlayerId)
                    .Where(s => s.Category == NexusUnitCategory.Capital)
                    .OrderBy(s =>
                        designs.TryGetValue(s.DesignId, out var d)
                            ? NexusHullBaselines.GetProfile(d).Cost
                            : 0
                    )
                    .ThenBy(s => s.RemainingHits)
                    .ToList();

                if (capitalStacks.Count == 0)
                    continue;

                Guid? currentDesignId = null;
                string currentDesignName = "";
                var countForCurrentDesign = 0;

                foreach (var stack in capitalStacks)
                {
                    if (deficit <= 0)
                        break;

                    if (currentDesignId.HasValue && currentDesignId.Value != stack.DesignId)
                    {
                        events.Add(
                            new NexusCapitalDisbandedEvent(
                                player.PlayerId,
                                currentDesignId.Value,
                                currentDesignName,
                                coord,
                                countForCurrentDesign
                            )
                        );
                        countForCurrentDesign = 0;
                    }

                    currentDesignId = stack.DesignId;
                    currentDesignName = designs.TryGetValue(stack.DesignId, out var d)
                        ? d.Name
                        : stack.DesignId.ToString();
                    var take = Math.Min(stack.Count, deficit);
                    system.RemoveUnits(player.PlayerId, stack.DesignId, take);
                    deficit -= take;
                    countForCurrentDesign += take;
                }

                if (currentDesignId.HasValue && countForCurrentDesign > 0)
                {
                    events.Add(
                        new NexusCapitalDisbandedEvent(
                            player.PlayerId,
                            currentDesignId.Value,
                            currentDesignName,
                            coord,
                            countForCurrentDesign
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
            s.GetPlayerStacks(playerId)
                .Where(st => st.Category == NexusUnitCategory.Capital)
                .Sum(st => st.Count)
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
        List<NexusResolveEvent> events,
        Dictionary<Guid, NexusUnitDesign> designs
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
                        movedStack.DesignId,
                        movedStack.Category,
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

        var affectedSystems = p1
            .PendingMoveOrders.Select(o => o.From)
            .Concat(p1.PendingMoveOrders.Select(o => o.To))
            .Concat(p2.PendingMoveOrders.Select(o => o.From))
            .Concat(p2.PendingMoveOrders.Select(o => o.To))
            .Distinct();

        foreach (var coord in affectedSystems)
        {
            var system = GetSystem(state, coord)!;
            UpdateSystemControl(system, p1.PlayerId, p2.PlayerId, events, designs);
        }
    }

    // ── Combat ────────────────────────────────────────────────────────────────

    private static void ResolveCombat(
        NexusState state,
        Guid player1Id,
        Guid player2Id,
        List<NexusResolveEvent> events,
        Random rng,
        Dictionary<Guid, NexusUnitDesign> designs
    )
    {
        foreach (var coord in NexusMap.SystemsInSpiralOrder)
        {
            var system = GetSystem(state, coord);
            if (system is null || !system.HasAnyUnits(player1Id) || !system.HasAnyUnits(player2Id))
                continue;

            ResolveSystemCombat(system, player1Id, player2Id, events, rng, designs);
        }
    }

    private static void ResolveSystemCombat(
        NexusSystemState system,
        Guid player1Id,
        Guid player2Id,
        List<NexusResolveEvent> events,
        Random rng,
        Dictionary<Guid, NexusUnitDesign> designs
    )
    {
        var units1 = ExpandUnits(system.GetPlayerStacks(player1Id), designs);
        var units2 = ExpandUnits(system.GetPlayerStacks(player2Id), designs);

        var phaseResults = ImmutableArray.CreateBuilder<NexusPhaseResult>();

        var contactResult = ResolveOneStep(
            system.Coord,
            units1,
            units2,
            player1Id,
            player2Id,
            rng,
            NexusCombatPhase.Contact
        );
        if (contactResult is not null)
            phaseResults.Add(contactResult);

        var battleResult = ResolveOneStep(
            system.Coord,
            units1,
            units2,
            player1Id,
            player2Id,
            rng,
            NexusCombatPhase.Battle
        );
        if (battleResult is not null)
            phaseResults.Add(battleResult);

        if (phaseResults.Count > 0)
            events.Add(
                new NexusCombatResultEvent(
                    system.Coord,
                    player1Id,
                    player2Id,
                    phaseResults.ToImmutable()
                )
            );

        var surviving1 = CollapseAlive(units1);
        var surviving2 = CollapseAlive(units2);

        var p1Cleared = units1.All(u => u.IsDestroyed);
        var p2Cleared = units2.All(u => u.IsDestroyed);

        if (p2Cleared && !p1Cleared)
            events.Add(new NexusSystemClearedEvent(system.Coord, player1Id));
        else if (p1Cleared && !p2Cleared)
            events.Add(new NexusSystemClearedEvent(system.Coord, player2Id));

        if (surviving1.Count > 0)
            system.Units[player1Id] = surviving1;
        else
            system.Units.Remove(player1Id);

        if (surviving2.Count > 0)
            system.Units[player2Id] = surviving2;
        else
            system.Units.Remove(player2Id);

        UpdateSystemControl(system, player1Id, player2Id, events, designs);
    }

    private static NexusPhaseResult? ResolveOneStep(
        HexCoord systemCoord,
        List<CombatUnit> units1,
        List<CombatUnit> units2,
        Guid player1Id,
        Guid player2Id,
        Random rng,
        NexusCombatPhase phase
    )
    {
        var alive1 = units1.Where(u => !u.IsDestroyed).ToList();
        var alive2 = units2.Where(u => !u.IsDestroyed).ToList();

        if (alive1.Count == 0 && alive2.Count == 0)
            return null;

        var pendingHits = new List<(CombatUnit Target, Guid TargetPlayerId, bool WasShielded)>();
        var attackRolls = new List<NexusCombatAttackRoll>();
        GatherAttacks(alive1, units2, player1Id, player2Id, rng, pendingHits, attackRolls, phase);
        GatherAttacks(alive2, units1, player2Id, player1Id, rng, pendingHits, attackRolls, phase);

        var losses = new Dictionary<(Guid PlayerId, Guid DesignId, string DesignName), int>();
        foreach (var (target, targetPlayerId, wasShielded) in pendingHits)
        {
            if (target.IsDestroyed)
                continue;

            if (!wasShielded)
                target.RemainingHits--;
            if (target.IsDestroyed)
            {
                var key = (targetPlayerId, target.Design.DesignId, target.Design.Name);
                losses[key] = losses.GetValueOrDefault(key) + 1;
            }
        }

        return new NexusPhaseResult(
            phase,
            losses
                .Select(kv => new NexusCombatLoss(
                    kv.Key.PlayerId,
                    kv.Key.DesignId,
                    kv.Key.DesignName,
                    kv.Value
                ))
                .ToImmutableArray(),
            attackRolls.ToImmutableArray()
        );
    }

    private static void GatherAttacks(
        List<CombatUnit> attackers,
        List<CombatUnit> targets,
        Guid attackerPlayerId,
        Guid targetPlayerId,
        Random rng,
        List<(CombatUnit Target, Guid TargetPlayerId, bool WasShielded)> pendingHits,
        List<NexusCombatAttackRoll> attackRolls,
        NexusCombatPhase phase
    )
    {
        foreach (var attacker in attackers)
        {
            if (attacker.IsDestroyed)
                continue;

            var profile = attacker.Profile;
            var tags = profile.Modules;
            var friendlyProfiles = attackers
                .Where(a => !a.IsDestroyed)
                .Select(a => a.Profile)
                .ToList();
            var commandBonus = NexusCombatSpec.GetCommandBonus(profile, friendlyProfiles);

            var eligible = targets
                .Where(t =>
                    !t.IsDestroyed
                    && NexusCombatSpec.GetHitThreshold(profile, t.Profile, phase) is not null
                )
                .ToList();

            if (eligible.Count == 0)
                continue;

            void PerformAttack(List<CombatUnit> pool)
            {
                if (pool.Count == 0)
                    return;

                var target = PickTargetByWeight(pool, attacker.Profile.Category, rng);
                var threshold = NexusCombatSpec
                    .GetHitThreshold(profile, target.Profile, phase, commandBonus)!
                    .Value;
                var roll = rng.Next(1, 7);
                var isHit = roll >= threshold;
                var wasShielded = false;
                if (isHit && target.ShieldActive && !tags.OfType<Disruptor>().Any())
                {
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
                        attacker.Design.DesignId,
                        attacker.Design.Name,
                        target.Design.DesignId,
                        target.Design.Name,
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

            for (var i = 0; i < profile.Attacks; i++)
                PerformAttack(eligible);

            foreach (var freeAttack in tags.OfType<Barrage>())
                PerformAttack(
                    eligible.Where(t => t.Profile.Category == freeAttack.Category).ToList()
                );
        }
    }

    // ── Control ───────────────────────────────────────────────────────────────

    private static void UpdateSystemControl(
        NexusSystemState system,
        Guid player1Id,
        Guid player2Id,
        List<NexusResolveEvent> events,
        Dictionary<Guid, NexusUnitDesign> designs
    )
    {
        if (system.IsNexus)
        {
            system.ControlOwner = null;
            return;
        }

        var p1HasPresence = system.HasAnyUnits(player1Id);
        var p2HasPresence = system.HasAnyUnits(player2Id);
        var p1HasPlanetary = system.HasControllingUnits(player1Id, designs);
        var p2HasPlanetary = system.HasControllingUnits(player2Id, designs);

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
        else
        {
            if (system.HomePlayerId.HasValue)
            {
                if (system.ControlOwner != system.HomePlayerId)
                {
                    system.ControlOwner = system.HomePlayerId;
                    events.Add(
                        new NexusPlanetaryControlEvent(system.Coord, system.HomePlayerId.Value)
                    );
                }
            }
            else if (system.ControlOwner is not null)
            {
                system.ControlOwner = null;
                events.Add(new NexusSystemUncontrolledEvent(system.Coord));
            }
        }
    }

    // ── Combat Helpers ────────────────────────────────────────────────────────

    private sealed class CombatUnit(NexusUnitDesign design)
    {
        public NexusUnitDesign Design { get; } = design;
        public NexusUnitProfile Profile { get; } = NexusHullBaselines.GetProfile(design);
        public int RemainingHits { get; set; } = NexusHullBaselines.GetProfile(design).Hits;
        public bool ShieldActive { get; set; } =
            NexusHullBaselines.GetProfile(design).Modules.OfType<Shield>().Any();
        public bool IsDestroyed => RemainingHits <= 0;
    }

    private static List<CombatUnit> ExpandUnits(
        IReadOnlyList<NexusUnitStack> stacks,
        Dictionary<Guid, NexusUnitDesign> designs
    )
    {
        var result = new List<CombatUnit>();
        foreach (var stack in stacks)
        {
            if (!designs.TryGetValue(stack.DesignId, out var design))
                continue;
            for (var i = 0; i < stack.Count; i++)
                result.Add(new CombatUnit(design) { RemainingHits = stack.RemainingHits });
        }
        return result;
    }

    private static List<NexusUnitStack> CollapseAlive(List<CombatUnit> units)
    {
        var grouped =
            new Dictionary<(Guid DesignId, NexusUnitCategory Category, int RemainingHits), int>();
        foreach (var unit in units.Where(u => !u.IsDestroyed))
        {
            var key = (unit.Design.DesignId, unit.Design.Hull, unit.RemainingHits);
            grouped[key] = grouped.GetValueOrDefault(key) + 1;
        }
        return grouped
            .Select(kv => new NexusUnitStack
            {
                DesignId = kv.Key.DesignId,
                Category = kv.Key.Category,
                RemainingHits = kv.Key.RemainingHits,
                Count = kv.Value,
            })
            .ToList();
    }

    private static CombatUnit PickTargetByWeight(
        List<CombatUnit> targets,
        NexusUnitCategory attackerCategory,
        Random rng
    )
    {
        var profiles = targets.Select(t => t.Profile).ToList();
        var weights = NexusCombatSpec.ComputeTargetWeights(profiles, attackerCategory);
        var totalWeight = weights.Sum();

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
            ComputeCapitalCount(state, player.PlayerId),
            isSelf ? player.Designs.ToImmutableArray() : null
        );

    private static ImmutableDictionary<Guid, ImmutableArray<NexusUnitStackGroup>> ProjectUnitStacks(
        NexusState state,
        NexusSystemState system,
        Dictionary<Guid, NexusUnitDesign> designs,
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

            var filtered = excludePlanetary
                ? stacks.Where(s => s.Category != NexusUnitCategory.Planetary)
                : stacks;

            var projected = filtered
                .Select(stack => new NexusUnitStackGroup(
                    stack.DesignId,
                    stack.Category,
                    stack.RemainingHits,
                    stack.Count,
                    designs.TryGetValue(stack.DesignId, out var d) ? d.Name : ""
                ))
                .OrderBy(stack => stack.DesignId)
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

    private static Dictionary<Guid, NexusUnitDesign> BuildDesignLookup(NexusState state)
    {
        var lookup = new Dictionary<Guid, NexusUnitDesign>();
        foreach (var design in state.Players.SelectMany(p => p.Designs))
            lookup.TryAdd(design.DesignId, design);
        return lookup;
    }

    private static NexusPlayerState? GetPlayer(NexusState state, Guid playerId) =>
        state.Players.FirstOrDefault(p => p.PlayerId == playerId);

    private static NexusSystemState? GetSystem(NexusState state, HexCoord coord) =>
        state.Systems.FirstOrDefault(s => s.Coord == coord);
}
