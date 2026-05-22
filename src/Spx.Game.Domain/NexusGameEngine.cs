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
            })
            .ToList();

        // Starting fleets (2 per player, at home)
        state.Fleets =
        [
            new NexusFleetState
            {
                FleetId = Guid.NewGuid(),
                OwnerId = redPlayerId,
                Position = NexusMap.RedHomeCoord,
            },
            new NexusFleetState
            {
                FleetId = Guid.NewGuid(),
                OwnerId = redPlayerId,
                Position = NexusMap.RedHomeCoord,
            },
            new NexusFleetState
            {
                FleetId = Guid.NewGuid(),
                OwnerId = bluePlayerId,
                Position = NexusMap.BlueHomeCoord,
            },
            new NexusFleetState
            {
                FleetId = Guid.NewGuid(),
                OwnerId = bluePlayerId,
                Position = NexusMap.BlueHomeCoord,
            },
        ];
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

        var playerFleets = state.Fleets.Where(f => f.OwnerId == command.PlayerId).ToList();
        var playerFleetIds = playerFleets.Select(f => f.FleetId).ToHashSet();
        var orderedFleetIds = new HashSet<Guid>();

        foreach (var order in command.FleetOrders)
        {
            var err = ValidateFleetOrder(
                state,
                command.PlayerId,
                order,
                playerFleets,
                playerFleetIds,
                orderedFleetIds
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
            var err = ValidateBeginNexusGate(command, player, playerFleets);
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
        List<NexusFleetState> playerFleets,
        HashSet<Guid> playerFleetIds,
        HashSet<Guid> orderedFleetIds
    )
    {
        if (!playerFleetIds.Contains(order.FleetId))
            return new NexusTurnOrdersRejected(
                $"Fleet {order.FleetId} not found or not owned by this player."
            );
        if (!orderedFleetIds.Add(order.FleetId))
            return new NexusTurnOrdersRejected($"Fleet {order.FleetId} has duplicate orders.");

        return order switch
        {
            NexusMoveOrder move => ValidateMoveOrder(state, playerId, move, playerFleets),
            NexusColonizeOrder colonize => ValidateColonizeOrder(state, colonize, playerFleets),
            _ => null,
        };
    }

    private static NexusTurnOrdersRejected? ValidateMoveOrder(
        NexusGameState state,
        Guid playerId,
        NexusMoveOrder move,
        List<NexusFleetState> playerFleets
    )
    {
        if (!NexusMap.IsValidCoord(move.Destination))
            return new NexusTurnOrdersRejected(
                $"Move destination {move.Destination} is not a valid map hex."
            );

        var fleet = playerFleets.First(f => f.FleetId == move.FleetId);
        var distance = fleet.Position.DistanceTo(move.Destination);

        if (distance == 0)
            return new NexusTurnOrdersRejected(
                $"Fleet {move.FleetId} move destination is the same as its current position."
            );
        if (distance == 1)
            return null; // normal adjacency move — always valid

        if (distance == 2)
        {
            var hasBonus = state.ActiveTradeRoutes.Any(r =>
                (r.Hex1 == fleet.Position && r.Owner1 == playerId)
                || (r.Hex2 == fleet.Position && r.Owner2 == playerId)
            );
            return hasBonus
                ? null
                : new NexusTurnOrdersRejected(
                    $"Fleet {move.FleetId} cannot move 2 hexes: not on an active trade-route endpoint."
                );
        }

        return new NexusTurnOrdersRejected(
            $"Fleet {move.FleetId} move distance {distance} exceeds maximum allowed."
        );
    }

    private static NexusTurnOrdersRejected? ValidateColonizeOrder(
        NexusGameState state,
        NexusColonizeOrder colonize,
        List<NexusFleetState> playerFleets
    )
    {
        var fleet = playerFleets.First(f => f.FleetId == colonize.FleetId);
        if (NexusMap.ByCoord[fleet.Position].IsNexus)
            return new NexusTurnOrdersRejected("Cannot colonize the Nexus hex.");

        var hexState = state.Hexes.First(h => h.Coord == fleet.Position);
        return hexState.ColonyOwnerId == fleet.OwnerId
            ? new NexusTurnOrdersRejected(
                $"Fleet {colonize.FleetId}: hex {fleet.Position} is already owned by you."
            )
            : null;
    }

    private static NexusTurnOrdersRejected? ValidateBuildFleet(NexusPlayerState player) =>
        player.RedCredits < BuildFleetCostPerColor
        || player.BlueCredits < BuildFleetCostPerColor
        || player.GoldCredits < BuildFleetCostPerColor
            ? new NexusTurnOrdersRejected(
                "Insufficient resources to build fleet (requires 2 Red + 2 Blue + 2 Gold)."
            )
            : null;

    private static NexusTurnOrdersRejected? ValidateBeginNexusGate(
        NexusTurnOrdersCommand command,
        NexusPlayerState player,
        List<NexusFleetState> playerFleets
    )
    {
        if (player.GateProgress == NexusGateProgress.Completed)
            return new NexusTurnOrdersRejected("Nexus Gate is already completed.");

        var movingFleetIds = command
            .FleetOrders.OfType<NexusMoveOrder>()
            .Select(o => o.FleetId)
            .ToHashSet();
        var stayingFleetOnNexus = playerFleets.Any(f =>
            f.Position == NexusMap.NexusCoord && !movingFleetIds.Contains(f.FleetId)
        );
        if (!stayingFleetOnNexus)
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
        ProcessIncome(state, red, blue, events);

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
                player.RedCredits -= BuildFleetCostPerColor;
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
        // Build lookup: fleetId -> MoveOrder
        var moveOrders = new Dictionary<Guid, NexusMoveOrder>();
        foreach (var player in new[] { red, blue })
        {
            foreach (var order in player.PendingFleetOrders.OfType<NexusMoveOrder>())
                moveOrders[order.FleetId] = order;
        }

        // Track which hexes were active trade-route endpoints for speed-bonus determination
        var speedBonusEndpoints = state
            .ActiveTradeRoutes.SelectMany<NexusTradeRoute, (HexCoord, Guid)>(r =>
                [(r.Hex1, r.Owner1), (r.Hex2, r.Owner2)]
            )
            .ToHashSet();

        // Apply all moves simultaneously
        foreach (var fleet in state.Fleets)
        {
            if (!moveOrders.TryGetValue(fleet.FleetId, out var move))
                continue;

            var from = fleet.Position;
            var to = move.Destination;
            var isSpeedBonus =
                from.DistanceTo(to) == 2 && speedBonusEndpoints.Contains((from, fleet.OwnerId));

            fleet.Position = to;

            var faction = GetFactionForPlayer(state, fleet.OwnerId);
            if (isSpeedBonus)
                events.Add(
                    new NexusSpeedBonusMoveEvent(
                        fleet.FleetId,
                        fleet.OwnerId,
                        faction,
                        from,
                        to,
                        from
                    )
                );
            else
                events.Add(new NexusMoveEvent(fleet.FleetId, fleet.OwnerId, faction, from, to));
        }

        // Determine contested hexes (both players have fleets there)
        var hexFleetCounts = state
            .Fleets.GroupBy(f => f.Position)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(f => f.OwnerId).ToDictionary(x => x.Key, x => x.Count())
            );

        var combatHexes = new HashSet<HexCoord>(
            hexFleetCounts.Where(kv => kv.Value.Count >= 2).Select(kv => kv.Key)
        );

        // Undefended entry: fleet moved into opponent-controlled hex with no opponent fleet
        foreach (var fleet in state.Fleets)
        {
            if (!moveOrders.ContainsKey(fleet.FleetId))
                continue; // didn't move

            if (combatHexes.Contains(fleet.Position))
                continue; // will be handled by combat

            var hexState = state.Hexes.First(h => h.Coord == fleet.Position);
            if (hexState.ColonyOwnerId is not null && hexState.ColonyOwnerId != fleet.OwnerId)
            {
                hexState.ColonyOwnerId = null;
                var faction = GetFactionForPlayer(state, fleet.OwnerId);
                events.Add(
                    new NexusUndefendedEntryEvent(
                        fleet.FleetId,
                        fleet.OwnerId,
                        faction,
                        fleet.Position
                    )
                );
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
            var fleetsHere = state.Fleets.Where(f => f.Position == hex).ToList();
            var redFleets = fleetsHere.Where(f => f.OwnerId == red.PlayerId).ToList();
            var blueFleets = fleetsHere.Where(f => f.OwnerId == blue.PlayerId).ToList();

            var redCount = redFleets.Count;
            var blueCount = blueFleets.Count;

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

            // Remove losing fleets (or all on mutual destruction)
            if (winnerId == red.PlayerId)
            {
                state.Fleets.RemoveAll(f => f.OwnerId == blue.PlayerId && f.Position == hex);
                RemoveFleets(state, redFleets, redLosses);
            }
            else if (winnerId == blue.PlayerId)
            {
                state.Fleets.RemoveAll(f => f.OwnerId == red.PlayerId && f.Position == hex);
                RemoveFleets(state, blueFleets, blueLosses);
            }
            else
            {
                // Mutual destruction
                state.Fleets.RemoveAll(f => f.Position == hex);
            }

            // Colony reverts to unclaimed after combat
            var hexState = state.Hexes.First(h => h.Coord == hex);
            hexState.ColonyOwnerId = null;
        }
    }

    private static void RemoveFleets(NexusGameState state, List<NexusFleetState> fleets, int count)
    {
        for (var i = 0; i < count; i++)
            state.Fleets.Remove(fleets[i]);
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

            var fleetOnNexus = state.Fleets.Any(f =>
                f.OwnerId == player.PlayerId && f.Position == NexusMap.NexusCoord
            );

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
                var fleet = state.Fleets.FirstOrDefault(f => f.FleetId == order.FleetId);
                if (fleet is null)
                    continue; // fleet was destroyed in combat

                var hex = fleet.Position;
                var faction = player.Faction;

                if (combatHexes.Contains(hex))
                {
                    events.Add(
                        new NexusColonizeFailedEvent(fleet.FleetId, player.PlayerId, faction, hex)
                    );
                    continue;
                }

                var hexState = state.Hexes.First(h => h.Coord == hex);
                hexState.ColonyOwnerId = player.PlayerId;

                var hexDef = NexusMap.ByCoord[hex];
                events.Add(
                    new NexusColonizeEvent(
                        fleet.FleetId,
                        player.PlayerId,
                        faction,
                        hex,
                        hexDef.Color
                    )
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
        List<NexusResolveEvent> events
    )
    {
        var movingFleetIds = new HashSet<Guid>(
            new[] { red, blue }.SelectMany(p =>
                p.PendingFleetOrders.OfType<NexusMoveOrder>().Select(o => o.FleetId)
            )
        );
        bool IsStaying(NexusFleetState f) =>
            !movingFleetIds.Contains(f.FleetId) && state.Fleets.Contains(f);

        var (newRoutes, newRouteKeys) = ComputeNewTradeRoutes(state, red, blue, IsStaying);
        EmitTradeRouteChangedEvents(state, newRoutes, newRouteKeys, events);
        state.ActiveTradeRoutes = newRoutes;

        var acc = new IncomeAccumulator();
        ComputeHexBaseIncome(state, red.PlayerId, blue.PlayerId, acc);

        var redStaying = GetStayingColonyPositions(state, red, IsStaying);
        var blueStaying = GetStayingColonyPositions(state, blue, IsStaying);
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

    private static (
        List<NexusTradeRoute> Routes,
        HashSet<(HexCoord, HexCoord)> Keys
    ) ComputeNewTradeRoutes(
        NexusGameState state,
        NexusPlayerState red,
        NexusPlayerState blue,
        Func<NexusFleetState, bool> isStaying
    )
    {
        var routes = new List<NexusTradeRoute>();
        var keys = new HashSet<(HexCoord, HexCoord)>();

        foreach (var player in new[] { red, blue })
        {
            var stayingPositions = state
                .Fleets.Where(f => f.OwnerId == player.PlayerId && isStaying(f))
                .Select(f => f.Position)
                .ToHashSet();

            var colonizingHexes = player
                .PendingFleetOrders.OfType<NexusColonizeOrder>()
                .Select(o => state.Fleets.FirstOrDefault(f => f.FleetId == o.FleetId)?.Position)
                .OfType<HexCoord?>()
                .Select(h => h!.Value)
                .ToHashSet();

            foreach (var pos in stayingPositions.Union(colonizingHexes))
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
        Func<NexusFleetState, bool> isStaying
    ) =>
        state
            .Fleets.Where(f =>
                f.OwnerId == player.PlayerId
                && isStaying(f)
                && state.Hexes.Any(h => h.Coord == f.Position && h.ColonyOwnerId == player.PlayerId)
            )
            .Select(f => f.Position)
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
            var newFleet = new NexusFleetState
            {
                FleetId = Guid.NewGuid(),
                OwnerId = player.PlayerId,
                Position = homeCoord,
            };
            state.Fleets.Add(newFleet);
            events.Add(
                new NexusFleetDeployedEvent(
                    newFleet.FleetId,
                    player.PlayerId,
                    player.Faction,
                    homeCoord
                )
            );
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

    private static ImmutableArray<NexusHexView> BuildHexViews(NexusGameState state)
    {
        var fleetsByHex = state
            .Fleets.GroupBy(f => f.Position)
            .ToDictionary(
                g => g.Key,
                g =>
                    g.Select(f => new NexusFleetView(
                            f.FleetId,
                            f.OwnerId,
                            GetFactionForPlayer(state, f.OwnerId)
                        ))
                        .ToImmutableArray()
            );

        return NexusMap
            .Hexes.Select(def =>
            {
                var hexState = state.Hexes.First(h => h.Coord == def.Coord);
                fleetsByHex.TryGetValue(def.Coord, out var fleets);
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
                    fleetsByHex.GetValueOrDefault(def.Coord, ImmutableArray<NexusFleetView>.Empty)
                );
            })
            .ToImmutableArray();
    }

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
