using System.Collections.Immutable;

namespace Spx.Game.Domain;

public static class NexusGameEngine
{
    private const int MaxRounds = 15;
    private const int BuildFleetCostPerColor = 2;
    private const int GateCostPerColorPerTurn = 4;
    private const int StartingFleets = 2;

    // -------------------------------------------------------------------------
    // Initialize
    // -------------------------------------------------------------------------

    public static void Initialize(NexusGameState state, InitializeNexusGameCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        if (command.FirstPlayer.PlayerId == command.SecondPlayer.PlayerId)
            throw new InvalidOperationException("A game session requires two distinct players.");

        // If already initialized with the same players, reset to start
        var sameRoster =
            state.RedPlayer is not null
            && state.BluePlayer is not null
            && (
                (
                    state.RedPlayer.PlayerId == command.FirstPlayer.PlayerId
                    && state.BluePlayer.PlayerId == command.SecondPlayer.PlayerId
                )
                || (
                    state.RedPlayer.PlayerId == command.SecondPlayer.PlayerId
                    && state.BluePlayer.PlayerId == command.FirstPlayer.PlayerId
                )
            );

        // Assign factions: randomise unless re-initialising the same roster
        Guid redPlayerId;
        Guid bluePlayerId;
        if (sameRoster)
        {
            redPlayerId = state.RedPlayer!.PlayerId;
            bluePlayerId = state.BluePlayer!.PlayerId;
        }
        else
        {
            var flip = Random.Shared.Next(2) == 0;
            redPlayerId = flip ? command.FirstPlayer.PlayerId : command.SecondPlayer.PlayerId;
            bluePlayerId = flip ? command.SecondPlayer.PlayerId : command.FirstPlayer.PlayerId;
        }

        state.RedPlayer = new NexusPlayerState
        {
            PlayerId = redPlayerId,
            Faction = NexusFactionColor.Red,
            IsActive = true,
        };
        state.BluePlayer = new NexusPlayerState
        {
            PlayerId = bluePlayerId,
            Faction = NexusFactionColor.Blue,
            IsActive = true,
        };

        state.RoundNumber = 1;
        state.Phase = NexusGamePhase.Planning;
        state.Completion = null;
        state.ResolveEvents = [];
        state.ActiveTradeRoutes = [];

        // Initialise hex ownership — pre-colonise home hexes
        state.Hexes = NexusMap
            .Hexes.Select(h => new NexusHexState
            {
                Coord = h.Coord,
                ColonyOwnerId = h.IsHome
                    ? (h.Color == NexusColonyColor.Red ? redPlayerId : bluePlayerId)
                    : null,
                RedFleets = h.Coord == NexusMap.RedHomeCoord ? StartingFleets : 0,
                BlueFleets = h.Coord == NexusMap.BlueHomeCoord ? StartingFleets : 0,
            })
            .ToList();
    }

    // -------------------------------------------------------------------------
    // SubmitOrders
    // -------------------------------------------------------------------------

    public static NexusTurnOrdersResult SubmitOrders(
        NexusGameState state,
        NexusTurnOrdersCommand command
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        if (state.Phase != NexusGamePhase.Planning)
            return new NexusTurnOrdersRejected("Game is not in the planning phase.");
        if (state.Completion is not null)
            return new NexusTurnOrdersRejected("Game is already ended.");
        if (command.ExpectedRoundNumber != state.RoundNumber)
            return new NexusTurnOrdersRejected(
                $"Round mismatch: expected {command.ExpectedRoundNumber}, current is {state.RoundNumber}."
            );

        var player = GetPlayer(state, command.PlayerId);
        if (player is null)
            return new NexusTurnOrdersRejected("Player not found.");
        if (!player.IsActive)
            return new NexusTurnOrdersRejected("Player is not active.");
        if (player.HasSubmittedOrders)
            return new NexusTurnOrdersRejected("Orders already submitted this round.");

        var assignedCounts = new Dictionary<HexCoord, int>();
        var colonizeHexes = new HashSet<HexCoord>();

        foreach (var order in command.FleetOrders)
        {
            var err = ValidateFleetOrder(
                state,
                command.PlayerId,
                order,
                player,
                assignedCounts,
                colonizeHexes
            );
            if (err is not null)
                return err;
        }

        if (command.BuildFleet)
        {
            var err = ValidateBuildFleet(player);
            if (err is not null)
                return err;
        }

        if (command.BeginNexusGate)
        {
            var err = ValidateBeginNexusGate(command, player, state);
            if (err is not null)
                return err;
        }

        player.PendingFleetOrders = command.FleetOrders.ToList();
        player.PendingBuildFleet = command.BuildFleet;
        player.PendingBeginNexusGate = command.BeginNexusGate;
        player.HasSubmittedOrders = true;

        var other = GetOtherPlayer(state, command.PlayerId);
        if (other is not null && other.IsActive && other.HasSubmittedOrders)
            Resolve(state);

        return new NexusTurnOrdersAccepted();
    }

    private static NexusTurnOrdersRejected? ValidateFleetOrder(
        NexusGameState state,
        Guid playerId,
        NexusFleetOrder order,
        NexusPlayerState player,
        Dictionary<HexCoord, int> assignedCounts,
        HashSet<HexCoord> colonizeHexes
    )
    {
        if (!NexusMap.IsValidCoord(order.From))
            return new NexusTurnOrdersRejected(
                $"Fleet order source {order.From} is not a valid map hex."
            );

        var hexState = state.Hexes.First(h => h.Coord == order.From);
        var fleetCount =
            player.Faction == NexusFactionColor.Red ? hexState.RedFleets : hexState.BlueFleets;

        if (fleetCount == 0)
            return new NexusTurnOrdersRejected($"No fleets at {order.From}.");

        return order switch
        {
            NexusMoveOrder move => ValidateMoveOrder(
                state,
                playerId,
                move,
                fleetCount,
                assignedCounts
            ),
            NexusColonizeOrder colonize => ValidateColonizeOrder(
                state,
                colonize,
                player,
                fleetCount,
                assignedCounts,
                colonizeHexes
            ),
            _ => null,
        };
    }

    private static NexusTurnOrdersRejected? ValidateMoveOrder(
        NexusGameState state,
        Guid playerId,
        NexusMoveOrder move,
        int fleetCount,
        Dictionary<HexCoord, int> assignedCounts
    )
    {
        if (move.Count <= 0)
            return new NexusTurnOrdersRejected("Move count must be at least 1.");

        var alreadyAssigned = assignedCounts.GetValueOrDefault(move.From, 0);
        if (alreadyAssigned + move.Count > fleetCount)
            return new NexusTurnOrdersRejected(
                $"Not enough fleets at {move.From}: requested {alreadyAssigned + move.Count} but only {fleetCount} available."
            );

        if (!NexusMap.IsValidCoord(move.To))
            return new NexusTurnOrdersRejected(
                $"Move destination {move.To} is not a valid map hex."
            );

        var distance = move.From.DistanceTo(move.To);

        if (distance == 0)
            return new NexusTurnOrdersRejected(
                $"Move destination is the same as its source hex {move.From}."
            );

        if (distance == 1)
        {
            assignedCounts[move.From] = alreadyAssigned + move.Count;
            return null;
        }

        if (distance == 2)
        {
            var hasBonus = state.ActiveTradeRoutes.Any(r =>
                (r.Hex1 == move.From && r.Owner1 == playerId)
                || (r.Hex2 == move.From && r.Owner2 == playerId)
            );
            if (!hasBonus)
                return new NexusTurnOrdersRejected(
                    $"Cannot move 2 hexes from {move.From}: not on an active trade-route endpoint."
                );

            assignedCounts[move.From] = alreadyAssigned + move.Count;
            return null;
        }

        return new NexusTurnOrdersRejected(
            $"Move distance {distance} from {move.From} exceeds maximum allowed."
        );
    }

    private static NexusTurnOrdersRejected? ValidateColonizeOrder(
        NexusGameState state,
        NexusColonizeOrder colonize,
        NexusPlayerState player,
        int fleetCount,
        Dictionary<HexCoord, int> assignedCounts,
        HashSet<HexCoord> colonizeHexes
    )
    {
        if (!colonizeHexes.Add(colonize.From))
            return new NexusTurnOrdersRejected($"Duplicate colonize order for {colonize.From}.");

        var assigned = assignedCounts.GetValueOrDefault(colonize.From, 0);
        if (assigned >= fleetCount)
            return new NexusTurnOrdersRejected(
                $"No fleets remaining at {colonize.From} to colonize (all assigned to move)."
            );

        if (NexusMap.ByCoord[colonize.From].IsNexus)
            return new NexusTurnOrdersRejected("Cannot colonize the Nexus hex.");

        var hexState = state.Hexes.First(h => h.Coord == colonize.From);
        return hexState.ColonyOwnerId == player.PlayerId
            ? new NexusTurnOrdersRejected($"Hex {colonize.From} is already owned by you.")
            : null;
    }

    private static NexusTurnOrdersRejected? ValidateBuildFleet(NexusPlayerState player)
    {
        var factionCost =
            player.Faction == NexusFactionColor.Red ? player.RedCredits : player.BlueCredits;
        var factionColor = player.Faction == NexusFactionColor.Red ? "Red" : "Blue";
        return factionCost < BuildFleetCostPerColor || player.GoldCredits < BuildFleetCostPerColor
            ? new NexusTurnOrdersRejected(
                $"Insufficient resources to build fleet (requires 2 {factionColor} + 2 Gold)."
            )
            : null;
    }

    private static NexusTurnOrdersRejected? ValidateBeginNexusGate(
        NexusTurnOrdersCommand command,
        NexusPlayerState player,
        NexusGameState state
    )
    {
        if (player.GateProgress == NexusGateProgress.Completed)
            return new NexusTurnOrdersRejected("Nexus Gate is already completed.");

        var nexusHex = state.Hexes.First(h => h.Coord == NexusMap.NexusCoord);
        var fleetCount =
            player.Faction == NexusFactionColor.Red ? nexusHex.RedFleets : nexusHex.BlueFleets;
        var movingFromNexus = command
            .FleetOrders.OfType<NexusMoveOrder>()
            .Where(o => o.From == NexusMap.NexusCoord)
            .Sum(o => o.Count);

        if (fleetCount - movingFromNexus <= 0)
            return new NexusTurnOrdersRejected(
                "Begin Nexus Gate requires a fleet staying on the Nexus hex."
            );

        return
            player.RedCredits < GateCostPerColorPerTurn
            || player.BlueCredits < GateCostPerColorPerTurn
            || player.GoldCredits < GateCostPerColorPerTurn
            ? new NexusTurnOrdersRejected(
                "Insufficient resources to begin/continue Nexus Gate (requires 4 Red + 4 Blue + 4 Gold)."
            )
            : null;
    }

    // -------------------------------------------------------------------------
    // Abandon
    // -------------------------------------------------------------------------

    public static void Abandon(NexusGameState state, Guid playerId)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Completion is not null)
            return;

        var player = GetPlayer(state, playerId);
        if (player is null)
            return;

        player.IsActive = false;
        var other = GetOtherPlayer(state, playerId);

        state.Completion = new NexusGameCompletion(
            NexusGameOutcome.Victory,
            WinnerId: other?.PlayerId
        );
        state.Phase = NexusGamePhase.Ended;
    }

    // -------------------------------------------------------------------------
    // BuildView
    // -------------------------------------------------------------------------

    public static NexusGameView BuildView(NexusGameState state, Guid gameId, Guid playerId)
    {
        ArgumentNullException.ThrowIfNull(state);

        var currentPlayer = GetPlayer(state, playerId);
        var opponentPlayer = GetOtherPlayer(state, playerId);

        return new NexusGameView(
            GameId: gameId,
            RoundNumber: state.RoundNumber,
            Phase: state.Phase,
            Hexes: BuildHexViews(state),
            ActiveTradeRoutes: state
                .ActiveTradeRoutes.Select(r => new NexusTradeRouteView(
                    r.Hex1,
                    r.Owner1,
                    GetFactionForPlayer(state, r.Owner1),
                    r.Hex2,
                    r.Owner2,
                    GetFactionForPlayer(state, r.Owner2)
                ))
                .ToImmutableArray(),
            CurrentPlayer: BuildPlayerView(state, currentPlayer, isSelf: true),
            OpponentPlayer: BuildPlayerView(state, opponentPlayer, isSelf: false),
            ResolveEvents: state.ResolveEvents.ToImmutableArray(),
            Completion: state.Completion
        );
    }

    // -------------------------------------------------------------------------
    // Resolve (private)
    // -------------------------------------------------------------------------

    private static void Resolve(NexusGameState state)
    {
        var events = new List<NexusResolveEvent>();
        var red = state.RedPlayer!;
        var blue = state.BluePlayer!;

        // Pre-compute staying hexes before moves alter fleet counts
        var redStayingHexes = ComputeStayingHexes(state, red);
        var blueStayingHexes = ComputeStayingHexes(state, blue);

        // Step 1: Deduct build/gate costs
        ProcessBuildAndGateDeductions(state, red, blue, events);

        // Step 2: Moves
        var combatHexes = ProcessMoves(state, red, blue, events);

        // Step 3: Combat + undefended entry + gate cancellation check
        ProcessCombatAndEntry(state, red, blue, events, combatHexes);
        ProcessGateOutcomes(state, red, blue, events);

        // Step 4: Colonization
        ProcessColonization(state, red, blue, events, combatHexes);

        // Step 5: Income (includes trade route computation)
        ProcessIncome(state, red, blue, events, redStayingHexes, blueStayingHexes);

        // Step 6: Deploy newly built fleets
        ProcessFleetDeployment(state, red, blue, events);

        state.ResolveEvents = events;

        // Win check
        if (CheckWinCondition(state, red, blue, events))
        {
            state.Phase = NexusGamePhase.Ended;
            return;
        }

        // Advance to next round
        state.RoundNumber++;
        foreach (var player in new[] { red, blue })
        {
            player.HasSubmittedOrders = false;
            player.PendingFleetOrders = [];
            player.PendingBuildFleet = false;
            player.PendingBeginNexusGate = false;
            player.PendingFleetDeployment = false;
        }
    }

    // -------------------------------------------------------------------------
    // Step 1: Build / gate deductions
    // -------------------------------------------------------------------------

    private static void ProcessBuildAndGateDeductions(
        NexusGameState state,
        NexusPlayerState red,
        NexusPlayerState blue,
        List<NexusResolveEvent> events
    )
    {
        foreach (var player in new[] { red, blue })
        {
            if (player.PendingBuildFleet)
            {
                if (player.Faction == NexusFactionColor.Red)
                    player.RedCredits -= BuildFleetCostPerColor;
                else
                    player.BlueCredits -= BuildFleetCostPerColor;
                player.GoldCredits -= BuildFleetCostPerColor;
                player.PendingFleetDeployment = true;
            }

            if (player.PendingBeginNexusGate)
            {
                player.GateProgressBeforeThisTurn = player.GateProgress;
                player.RedCredits -= GateCostPerColorPerTurn;
                player.BlueCredits -= GateCostPerColorPerTurn;
                player.GoldCredits -= GateCostPerColorPerTurn;

                if (player.GateProgress == NexusGateProgress.None)
                {
                    player.GateProgress = NexusGateProgress.Started;
                    events.Add(
                        new NexusGateBegunEvent(
                            player.PlayerId,
                            player.Faction,
                            NexusMap.NexusCoord,
                            GateCostPerColorPerTurn,
                            GateCostPerColorPerTurn,
                            GateCostPerColorPerTurn
                        )
                    );
                }
                else if (player.GateProgress == NexusGateProgress.Started)
                {
                    player.GateProgress = NexusGateProgress.Completed;
                    events.Add(
                        new NexusGateProgressedEvent(
                            player.PlayerId,
                            player.Faction,
                            NexusMap.NexusCoord,
                            GateCostPerColorPerTurn,
                            GateCostPerColorPerTurn,
                            GateCostPerColorPerTurn
                        )
                    );
                }
            }
        }
    }

    // -------------------------------------------------------------------------
    // Step 2: Moves
    // -------------------------------------------------------------------------

    private static HashSet<HexCoord> ProcessMoves(
        NexusGameState state,
        NexusPlayerState red,
        NexusPlayerState blue,
        List<NexusResolveEvent> events
    )
    {
        // Track which hexes were active trade-route endpoints for speed-bonus determination
        var speedBonusEndpoints = state
            .ActiveTradeRoutes.SelectMany<NexusTradeRoute, (HexCoord, Guid)>(r =>
                [(r.Hex1, r.Owner1), (r.Hex2, r.Owner2)]
            )
            .ToHashSet();

        // Apply all move orders simultaneously
        foreach (var player in new[] { red, blue })
        {
            foreach (var move in player.PendingFleetOrders.OfType<NexusMoveOrder>())
            {
                var fromState = state.Hexes.First(h => h.Coord == move.From);
                var toState = state.Hexes.First(h => h.Coord == move.To);

                if (player.Faction == NexusFactionColor.Red)
                {
                    fromState.RedFleets -= move.Count;
                    toState.RedFleets += move.Count;
                }
                else
                {
                    fromState.BlueFleets -= move.Count;
                    toState.BlueFleets += move.Count;
                }

                var isSpeedBonus =
                    move.From.DistanceTo(move.To) == 2
                    && speedBonusEndpoints.Contains((move.From, player.PlayerId));

                if (isSpeedBonus)
                    events.Add(
                        new NexusSpeedBonusMoveEvent(
                            player.PlayerId,
                            player.Faction,
                            move.From,
                            move.To,
                            move.From
                        )
                    );
                else
                    events.Add(
                        new NexusMoveEvent(player.PlayerId, player.Faction, move.From, move.To)
                    );
            }
        }

        // Determine contested hexes (both players have fleets there)
        var combatHexes = new HashSet<HexCoord>(
            state.Hexes.Where(h => h.RedFleets > 0 && h.BlueFleets > 0).Select(h => h.Coord)
        );

        // Undefended entry: fleet moved into opponent-controlled hex with no opponent fleet
        foreach (var player in new[] { red, blue })
        {
            foreach (var move in player.PendingFleetOrders.OfType<NexusMoveOrder>())
            {
                if (combatHexes.Contains(move.To))
                    continue; // will be handled by combat

                var hexState = state.Hexes.First(h => h.Coord == move.To);
                if (hexState.ColonyOwnerId is not null && hexState.ColonyOwnerId != player.PlayerId)
                {
                    hexState.ColonyOwnerId = null;
                    events.Add(
                        new NexusUndefendedEntryEvent(player.PlayerId, player.Faction, move.To)
                    );
                }
            }
        }

        return combatHexes;
    }

    // -------------------------------------------------------------------------
    // Step 3: Combat
    // -------------------------------------------------------------------------

    private static void ProcessCombatAndEntry(
        NexusGameState state,
        NexusPlayerState red,
        NexusPlayerState blue,
        List<NexusResolveEvent> events,
        HashSet<HexCoord> combatHexes
    )
    {
        foreach (var hex in combatHexes)
        {
            var hexState = state.Hexes.First(h => h.Coord == hex);
            var redCount = hexState.RedFleets;
            var blueCount = hexState.BlueFleets;

            Guid? winnerId;
            int redLosses;
            int blueLosses;

            if (redCount > blueCount)
            {
                winnerId = red.PlayerId;
                blueLosses = blueCount;
                redLosses = blueCount;
            }
            else if (blueCount > redCount)
            {
                winnerId = blue.PlayerId;
                redLosses = redCount;
                blueLosses = redCount;
            }
            else
            {
                winnerId = null;
                redLosses = redCount;
                blueLosses = blueCount;
            }

            events.Add(
                new NexusCombatEvent(
                    hex,
                    red.PlayerId,
                    NexusFactionColor.Red,
                    redCount,
                    blue.PlayerId,
                    NexusFactionColor.Blue,
                    blueCount,
                    winnerId,
                    redLosses,
                    blueLosses
                )
            );

            if (winnerId == red.PlayerId)
            {
                hexState.BlueFleets = 0;
                hexState.RedFleets -= redLosses;
            }
            else if (winnerId == blue.PlayerId)
            {
                hexState.RedFleets = 0;
                hexState.BlueFleets -= blueLosses;
            }
            else
            {
                hexState.RedFleets = 0;
                hexState.BlueFleets = 0;
            }

            // Colony reverts to unclaimed after combat
            hexState.ColonyOwnerId = null;
        }
    }

    // -------------------------------------------------------------------------
    // Gate outcome check (after combat)
    // -------------------------------------------------------------------------

    private static void ProcessGateOutcomes(
        NexusGameState state,
        NexusPlayerState red,
        NexusPlayerState blue,
        List<NexusResolveEvent> events
    )
    {
        foreach (var player in new[] { red, blue })
        {
            if (!player.PendingBeginNexusGate)
                continue;

            var nexusHex = state.Hexes.First(h => h.Coord == NexusMap.NexusCoord);
            var fleetOnNexus =
                player.Faction == NexusFactionColor.Red
                    ? nexusHex.RedFleets > 0
                    : nexusHex.BlueFleets > 0;

            if (!fleetOnNexus)
            {
                // Fleet destroyed — cancel gate
                player.GateProgress = player.GateProgressBeforeThisTurn;
                events.Add(
                    new NexusGateCancelledEvent(
                        player.PlayerId,
                        player.Faction,
                        NexusMap.NexusCoord
                    )
                );
            }
            // If fleet survived, gate progress was already advanced in step 1
        }
    }

    // -------------------------------------------------------------------------
    // Step 4: Colonization
    // -------------------------------------------------------------------------

    private static void ProcessColonization(
        NexusGameState state,
        NexusPlayerState red,
        NexusPlayerState blue,
        List<NexusResolveEvent> events,
        HashSet<HexCoord> combatHexes
    )
    {
        foreach (var player in new[] { red, blue })
        {
            foreach (var order in player.PendingFleetOrders.OfType<NexusColonizeOrder>())
            {
                var hex = order.From;
                var hexState = state.Hexes.First(h => h.Coord == hex);
                var fleetCount =
                    player.Faction == NexusFactionColor.Red
                        ? hexState.RedFleets
                        : hexState.BlueFleets;

                if (fleetCount == 0)
                    continue; // fleet destroyed in combat

                if (combatHexes.Contains(hex))
                {
                    events.Add(new NexusColonizeFailedEvent(player.PlayerId, player.Faction, hex));
                    continue;
                }

                hexState.ColonyOwnerId = player.PlayerId;
                var hexDef = NexusMap.ByCoord[hex];
                events.Add(
                    new NexusColonizeEvent(player.PlayerId, player.Faction, hex, hexDef.Color)
                );
            }
        }
    }

    // -------------------------------------------------------------------------
    // Step 5: Income + trade routes
    // -------------------------------------------------------------------------

    private static void ProcessIncome(
        NexusGameState state,
        NexusPlayerState red,
        NexusPlayerState blue,
        List<NexusResolveEvent> events,
        HashSet<HexCoord> redStayingHexes,
        HashSet<HexCoord> blueStayingHexes
    )
    {
        // Filter staying hexes to those where the fleet survived combat
        var redSurviving = redStayingHexes
            .Where(h => state.Hexes.First(hs => hs.Coord == h).RedFleets > 0)
            .ToHashSet();
        var blueSurviving = blueStayingHexes
            .Where(h => state.Hexes.First(hs => hs.Coord == h).BlueFleets > 0)
            .ToHashSet();

        var (newRoutes, newRouteKeys) = ComputeNewTradeRoutes(
            state,
            red,
            blue,
            redSurviving,
            blueSurviving
        );
        EmitTradeRouteChangedEvents(state, newRoutes, newRouteKeys, events);
        state.ActiveTradeRoutes = newRoutes;

        var acc = new IncomeAccumulator();
        ComputeHexBaseIncome(state, red.PlayerId, blue.PlayerId, acc);

        var redStaying = GetStayingColonyPositions(state, red, redSurviving);
        var blueStaying = GetStayingColonyPositions(state, blue, blueSurviving);
        ComputeRouteIncome(newRoutes, redStaying, blueStaying, red.PlayerId, acc);

        red.RedCredits += acc.RedR;
        red.BlueCredits += acc.RedB;
        red.GoldCredits += acc.RedG;
        blue.RedCredits += acc.BlueR;
        blue.BlueCredits += acc.BlueB;
        blue.GoldCredits += acc.BlueG;

        if (acc.RedR + acc.RedB + acc.RedG > 0)
            events.Add(
                new NexusIncomeEvent(
                    red.PlayerId,
                    NexusFactionColor.Red,
                    acc.RedR,
                    acc.RedB,
                    acc.RedG
                )
            );
        if (acc.BlueR + acc.BlueB + acc.BlueG > 0)
            events.Add(
                new NexusIncomeEvent(
                    blue.PlayerId,
                    NexusFactionColor.Blue,
                    acc.BlueR,
                    acc.BlueB,
                    acc.BlueG
                )
            );
    }

    private static HashSet<HexCoord> ComputeStayingHexes(
        NexusGameState state,
        NexusPlayerState player
    )
    {
        var movingCounts = player
            .PendingFleetOrders.OfType<NexusMoveOrder>()
            .GroupBy(o => o.From)
            .ToDictionary(g => g.Key, g => g.Sum(o => o.Count));

        return state
            .Hexes.Where(h =>
            {
                var total = player.Faction == NexusFactionColor.Red ? h.RedFleets : h.BlueFleets;
                var moving = movingCounts.GetValueOrDefault(h.Coord, 0);
                return total - moving > 0;
            })
            .Select(h => h.Coord)
            .ToHashSet();
    }

    private static (
        List<NexusTradeRoute> Routes,
        HashSet<(HexCoord, HexCoord)> Keys
    ) ComputeNewTradeRoutes(
        NexusGameState state,
        NexusPlayerState red,
        NexusPlayerState blue,
        HashSet<HexCoord> redSurvivingStaying,
        HashSet<HexCoord> blueSurvivingStaying
    )
    {
        var routes = new List<NexusTradeRoute>();
        var keys = new HashSet<(HexCoord, HexCoord)>();

        foreach (
            var (player, staying) in new[]
            {
                (red, redSurvivingStaying),
                (blue, blueSurvivingStaying),
            }
        )
        {
            foreach (var pos in staying)
                AddTradeRoutesForPosition(state, player, pos, routes, keys);
        }

        return (routes, keys);
    }

    private static void AddTradeRoutesForPosition(
        NexusGameState state,
        NexusPlayerState player,
        HexCoord pos,
        List<NexusTradeRoute> routes,
        HashSet<(HexCoord, HexCoord)> keys
    )
    {
        var hexState = state.Hexes.First(h => h.Coord == pos);
        if (hexState.ColonyOwnerId != player.PlayerId)
            return;

        var other = GetOtherPlayer(state, player.PlayerId);
        if (other is null)
            return;

        foreach (var neighbour in pos.GetNeighbours())
        {
            if (!NexusMap.IsValidCoord(neighbour))
                continue;
            var neighbourHex = state.Hexes.First(h => h.Coord == neighbour);
            if (neighbourHex.ColonyOwnerId != other.PlayerId)
                continue;

            var (h1, h2) =
                pos.Q < neighbour.Q || (pos.Q == neighbour.Q && pos.R < neighbour.R)
                    ? (pos, neighbour)
                    : (neighbour, pos);
            if (keys.Add((h1, h2)))
                routes.Add(
                    new NexusTradeRoute(
                        h1,
                        GetOwnerOfHex(state, h1)!.Value,
                        h2,
                        GetOwnerOfHex(state, h2)!.Value
                    )
                );
        }
    }

    private static void EmitTradeRouteChangedEvents(
        NexusGameState state,
        List<NexusTradeRoute> newRoutes,
        HashSet<(HexCoord, HexCoord)> newRouteKeys,
        List<NexusResolveEvent> events
    )
    {
        var previousKeys = state
            .ActiveTradeRoutes.Select(r =>
            {
                var key =
                    r.Hex1.Q < r.Hex2.Q || (r.Hex1.Q == r.Hex2.Q && r.Hex1.R < r.Hex2.R)
                        ? (r.Hex1, r.Hex2)
                        : (r.Hex2, r.Hex1);
                return key;
            })
            .ToHashSet();

        foreach (var route in newRoutes)
        {
            var key =
                route.Hex1.Q < route.Hex2.Q
                || (route.Hex1.Q == route.Hex2.Q && route.Hex1.R < route.Hex2.R)
                    ? (route.Hex1, route.Hex2)
                    : (route.Hex2, route.Hex1);
            if (!previousKeys.Contains(key))
                events.Add(
                    new NexusTradeRouteOpenedEvent(
                        route.Hex1,
                        route.Owner1,
                        GetFactionForPlayer(state, route.Owner1),
                        route.Hex2,
                        route.Owner2,
                        GetFactionForPlayer(state, route.Owner2)
                    )
                );
        }

        foreach (var old in state.ActiveTradeRoutes)
        {
            var key =
                old.Hex1.Q < old.Hex2.Q || (old.Hex1.Q == old.Hex2.Q && old.Hex1.R < old.Hex2.R)
                    ? (old.Hex1, old.Hex2)
                    : (old.Hex2, old.Hex1);
            if (!newRouteKeys.Contains(key))
                events.Add(
                    new NexusTradeRouteClosedEvent(
                        old.Hex1,
                        old.Owner1,
                        GetFactionForPlayer(state, old.Owner1),
                        old.Hex2,
                        old.Owner2,
                        GetFactionForPlayer(state, old.Owner2)
                    )
                );
        }
    }

    private static void ComputeHexBaseIncome(
        NexusGameState state,
        Guid redPlayerId,
        Guid bluePlayerId,
        IncomeAccumulator acc
    )
    {
        foreach (var hexState in state.Hexes)
        {
            if (hexState.ColonyOwnerId is null)
                continue;
            var hexDef = NexusMap.ByCoord[hexState.Coord];
            if (hexDef.Color == NexusColonyColor.None)
                continue;

            // Opposing-color colonies generate nothing — only deny opponent income.
            // Gold is neutral: either player earns it.
            if (hexDef.Color != NexusColonyColor.Gold)
            {
                var ownerColor =
                    hexState.ColonyOwnerId == redPlayerId
                        ? NexusColonyColor.Red
                        : NexusColonyColor.Blue;
                if (hexDef.Color != ownerColor)
                    continue;
            }

            var amount = hexDef.IsHome ? 2 : 1;
            acc.Apply(hexState.ColonyOwnerId.Value, redPlayerId, hexDef.Color, amount);
        }
    }

    private static HashSet<HexCoord> GetStayingColonyPositions(
        NexusGameState state,
        NexusPlayerState player,
        HashSet<HexCoord> survivingStayingHexes
    ) =>
        survivingStayingHexes
            .Where(pos =>
                state.Hexes.Any(h => h.Coord == pos && h.ColonyOwnerId == player.PlayerId)
            )
            .ToHashSet();

    private static void ComputeRouteIncome(
        List<NexusTradeRoute> routes,
        HashSet<HexCoord> redStaying,
        HashSet<HexCoord> blueStaying,
        Guid redPlayerId,
        IncomeAccumulator acc
    )
    {
        foreach (var route in routes)
        {
            var hex1Def = NexusMap.ByCoord[route.Hex1];
            var hex2Def = NexusMap.ByCoord[route.Hex2];
            var owner1Income =
                redStaying.Contains(route.Hex1) || blueStaying.Contains(route.Hex1) ? 2 : 1;
            var owner2Income =
                redStaying.Contains(route.Hex2) || blueStaying.Contains(route.Hex2) ? 2 : 1;
            acc.Apply(route.Owner1, redPlayerId, hex2Def.Color, owner1Income);
            acc.Apply(route.Owner2, redPlayerId, hex1Def.Color, owner2Income);
        }
    }

    private sealed class IncomeAccumulator
    {
        public int RedR,
            RedB,
            RedG,
            BlueR,
            BlueB,
            BlueG;

        public void Apply(Guid ownerId, Guid redPlayerId, NexusColonyColor color, int amount)
        {
            if (ownerId == redPlayerId)
                ApplyRed(color, amount);
            else
                ApplyBlue(color, amount);
        }

        private void ApplyRed(NexusColonyColor color, int amount)
        {
            switch (color)
            {
                case NexusColonyColor.Red:
                    RedR += amount;
                    break;
                case NexusColonyColor.Blue:
                    RedB += amount;
                    break;
                case NexusColonyColor.Gold:
                    RedG += amount;
                    break;
            }
        }

        private void ApplyBlue(NexusColonyColor color, int amount)
        {
            switch (color)
            {
                case NexusColonyColor.Red:
                    BlueR += amount;
                    break;
                case NexusColonyColor.Blue:
                    BlueB += amount;
                    break;
                case NexusColonyColor.Gold:
                    BlueG += amount;
                    break;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Step 6: Fleet deployment
    // -------------------------------------------------------------------------

    private static void ProcessFleetDeployment(
        NexusGameState state,
        NexusPlayerState red,
        NexusPlayerState blue,
        List<NexusResolveEvent> events
    )
    {
        foreach (var player in new[] { red, blue })
        {
            if (!player.PendingFleetDeployment)
                continue;

            var homeCoord = NexusMap.GetHomeCoord(player.Faction);
            var homeHex = state.Hexes.First(h => h.Coord == homeCoord);
            if (player.Faction == NexusFactionColor.Red)
                homeHex.RedFleets++;
            else
                homeHex.BlueFleets++;

            events.Add(new NexusFleetDeployedEvent(player.PlayerId, player.Faction, homeCoord));
        }
    }

    // -------------------------------------------------------------------------
    // Win check
    // -------------------------------------------------------------------------

    private static bool CheckWinCondition(
        NexusGameState state,
        NexusPlayerState red,
        NexusPlayerState blue,
        List<NexusResolveEvent> events
    )
    {
        var redCompleted =
            red.GateProgress == NexusGateProgress.Completed && red.PendingBeginNexusGate;
        var blueCompleted =
            blue.GateProgress == NexusGateProgress.Completed && blue.PendingBeginNexusGate;

        if (redCompleted || blueCompleted)
        {
            if (redCompleted && blueCompleted)
            {
                events.Add(
                    new NexusDrawEvent("Both players completed the Nexus Gate simultaneously.")
                );
                state.Completion = new NexusGameCompletion(NexusGameOutcome.Draw, WinnerId: null);
            }
            else
            {
                var winner = redCompleted ? red : blue;
                events.Add(new NexusVictoryEvent(winner.PlayerId, winner.Faction));
                state.Completion = new NexusGameCompletion(
                    NexusGameOutcome.Victory,
                    winner.PlayerId
                );
            }
            return true;
        }

        // Round 15 tiebreaker
        if (state.RoundNumber >= MaxRounds)
        {
            var redSystems = state.Hexes.Count(h => h.ColonyOwnerId == red.PlayerId);
            var blueSystems = state.Hexes.Count(h => h.ColonyOwnerId == blue.PlayerId);

            if (redSystems > blueSystems)
            {
                events.Add(
                    new NexusTiebreakerVictoryEvent(
                        red.PlayerId,
                        NexusFactionColor.Red,
                        redSystems,
                        blueSystems
                    )
                );
                state.Completion = new NexusGameCompletion(NexusGameOutcome.Victory, red.PlayerId);
            }
            else if (blueSystems > redSystems)
            {
                events.Add(
                    new NexusTiebreakerVictoryEvent(
                        blue.PlayerId,
                        NexusFactionColor.Blue,
                        blueSystems,
                        redSystems
                    )
                );
                state.Completion = new NexusGameCompletion(NexusGameOutcome.Victory, blue.PlayerId);
            }
            else
            {
                events.Add(new NexusTiebreakerDrawEvent(redSystems));
                state.Completion = new NexusGameCompletion(NexusGameOutcome.Draw, WinnerId: null);
            }
            return true;
        }

        return false;
    }

    // -------------------------------------------------------------------------
    // View helpers
    // -------------------------------------------------------------------------

    private static ImmutableArray<NexusHexView> BuildHexViews(NexusGameState state) =>
        NexusMap
            .Hexes.Select(def =>
            {
                var hexState = state.Hexes.First(h => h.Coord == def.Coord);
                NexusFactionColor? ownerFaction = hexState.ColonyOwnerId is { } ownerId
                    ? GetFactionForPlayer(state, ownerId)
                    : null;

                return new NexusHexView(
                    def.Coord,
                    def.Color,
                    def.IsNexus,
                    def.IsHome,
                    hexState.ColonyOwnerId,
                    ownerFaction,
                    hexState.RedFleets,
                    hexState.BlueFleets
                );
            })
            .ToImmutableArray();

    private static NexusPlayerView BuildPlayerView(
        NexusGameState state,
        NexusPlayerState? player,
        bool isSelf
    )
    {
        if (player is null)
        {
            return new NexusPlayerView(
                Guid.Empty,
                NexusFactionColor.Red,
                0,
                0,
                0,
                NexusGateProgress.None,
                false,
                false,
                null,
                false,
                false
            );
        }

        return new NexusPlayerView(
            player.PlayerId,
            player.Faction,
            player.RedCredits,
            player.BlueCredits,
            player.GoldCredits,
            player.GateProgress,
            player.HasSubmittedOrders,
            player.IsActive,
            isSelf
                ? player.PendingFleetOrders.ToImmutableArray()
                : (ImmutableArray<NexusFleetOrder>?)null,
            isSelf && player.PendingBuildFleet,
            isSelf && player.PendingBeginNexusGate
        );
    }

    // -------------------------------------------------------------------------
    // State helpers
    // -------------------------------------------------------------------------

    private static NexusPlayerState? GetPlayer(NexusGameState state, Guid playerId) =>
        state.RedPlayer?.PlayerId == playerId ? state.RedPlayer
        : state.BluePlayer?.PlayerId == playerId ? state.BluePlayer
        : null;

    private static NexusPlayerState? GetOtherPlayer(NexusGameState state, Guid playerId) =>
        state.RedPlayer?.PlayerId == playerId ? state.BluePlayer : state.RedPlayer;

    private static NexusFactionColor GetFactionForPlayer(NexusGameState state, Guid playerId) =>
        state.RedPlayer?.PlayerId == playerId ? NexusFactionColor.Red : NexusFactionColor.Blue;

    private static Guid? GetOwnerOfHex(NexusGameState state, HexCoord coord) =>
        state.Hexes.First(h => h.Coord == coord).ColonyOwnerId;
}
