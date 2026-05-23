using System.Collections.Immutable;
using System.Diagnostics;

namespace Spx.Game.Domain;

public static class NexusGameEngine
{
    private const int MaxRounds = 15;
    private const int BuildFleetCostPerColor = 2;
    private const int StartingFleets = 2;

    private static int GateCostForState(NexusGameState state) => 2 + state.Players.Count;

    private static NexusMapLayout GetMap(NexusGameState state) =>
        NexusMap.ForPlayerCount(state.Players.Count);

    // -------------------------------------------------------------------------
    // Initialize
    // -------------------------------------------------------------------------

    public static void Initialize(NexusGameState state, InitializeNexusGameCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        if (command.Players.Length < 2 || command.Players.Length > 4)
            throw new InvalidOperationException("A game session requires 2\u20134 players.");

        if (command.Players.Select(p => p.PlayerId).Distinct().Count() != command.Players.Length)
            throw new InvalidOperationException(
                "All players in a game session must have distinct IDs."
            );

        // If already initialized with the same roster, keep faction assignments
        var commandIds = command.Players.Select(p => p.PlayerId).ToHashSet();
        var sameRoster =
            state.Players.Count == command.Players.Length
            && state.Players.All(p => commandIds.Contains(p.PlayerId));

        List<NexusFactionColor> assignedFactions;
        if (sameRoster)
        {
            // Preserve existing assignments in command order
            assignedFactions = command
                .Players.Select(p => state.Players.First(s => s.PlayerId == p.PlayerId).Faction)
                .ToList();
        }
        else
        {
            var factions = new List<NexusFactionColor>
            {
                NexusFactionColor.Red,
                NexusFactionColor.Blue,
                NexusFactionColor.Green,
                NexusFactionColor.Yellow,
            };
            if (command.Players.Length > 2)
                Shuffle(factions);
            assignedFactions = factions.Take(command.Players.Length).ToList();
        }

        state.Players = command
            .Players.Select(
                (p, i) =>
                    new NexusPlayerState
                    {
                        PlayerId = p.PlayerId,
                        Faction = assignedFactions[i],
                        IsActive = true,
                    }
            )
            .ToList();

        state.RoundNumber = 1;
        state.Phase = NexusGamePhase.Planning;
        state.Completion = null;
        state.ResolveEvents = [];
        state.ActiveTradeRoutes = [];

        var map = GetMap(state);

        // Initialise hex ownership — pre-colonise home hexes
        state.Hexes = map
            .Hexes.Select(h =>
            {
                var hexState = new NexusHexState { Coord = h.Coord };
                if (h.IsHome && h.HomeFaction.HasValue)
                {
                    var owner = state.Players.First(p => p.Faction == h.HomeFaction.Value);
                    hexState.ColonyOwnerId = owner.PlayerId;
                    hexState.SetFleets(h.HomeFaction.Value, StartingFleets);
                }
                return hexState;
            })
            .ToList();
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
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

        var allOthersSubmitted = state
            .Players.Where(p => p.IsActive && p.PlayerId != command.PlayerId)
            .All(p => p.HasSubmittedOrders);
        if (allOthersSubmitted)
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

        var hexState = GetHex(state, order.From);
        var fleetCount = hexState.GetFleets(player.Faction);

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

        var hexState = GetHex(state, colonize.From);
        return hexState.ColonyOwnerId == player.PlayerId
            ? new NexusTurnOrdersRejected($"Hex {colonize.From} is already owned by you.")
            : null;
    }

    private static NexusTurnOrdersRejected? ValidateBuildFleet(NexusPlayerState player)
    {
        var factionColor = player.Faction.ToColonyColor();
        var factionCredits = player.GetCredits(factionColor);
        var goldCredits = player.GetCredits(NexusColonyColor.Gold);
        return factionCredits < BuildFleetCostPerColor || goldCredits < BuildFleetCostPerColor
            ? new NexusTurnOrdersRejected(
                $"Insufficient resources to build fleet (requires {BuildFleetCostPerColor} {factionColor} + {BuildFleetCostPerColor} Gold)."
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

        var map = GetMap(state);
        var nexusHex = GetHex(state, map.NexusCoord);
        var fleetCount = nexusHex.GetFleets(player.Faction);
        var movingFromNexus = command
            .FleetOrders.OfType<NexusMoveOrder>()
            .Where(o => o.From == map.NexusCoord)
            .Sum(o => o.Count);

        if (fleetCount - movingFromNexus <= 0)
            return new NexusTurnOrdersRejected(
                "Begin Nexus Gate requires a fleet staying on the Nexus hex."
            );

        var gateCost = GateCostForState(state);
        var resourceColors = map.ResourceColors;

        foreach (var color in resourceColors)
        {
            if (player.GetCredits(color) < gateCost)
                return new NexusTurnOrdersRejected(
                    $"Insufficient resources to begin/continue Nexus Gate (requires {gateCost} of each resource type)."
                );
        }

        return null;
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
        var activePlayers = state.Players.Where(p => p.IsActive).ToList();
        if (activePlayers.Count == 1)
        {
            state.Completion = new NexusGameCompletion(
                NexusGameOutcome.Victory,
                WinnerId: activePlayers[0].PlayerId
            );
            state.Phase = NexusGamePhase.Ended;
        }
        // If 2+ active remain, game continues
    }

    // -------------------------------------------------------------------------
    // BuildView
    // -------------------------------------------------------------------------

    public static NexusGameView BuildView(NexusGameState state, Guid gameId, Guid playerId)
    {
        ArgumentNullException.ThrowIfNull(state);

        var currentPlayer = GetPlayer(state, playerId);

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
            Opponents: state
                .Players.Where(p => p.PlayerId != playerId)
                .Select(p => BuildPlayerView(state, p, isSelf: false))
                .ToImmutableArray(),
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
        var players = state.Players.Where(p => p.IsActive).ToList();
        var map = GetMap(state);

        // Pre-compute staying hexes before moves alter fleet counts
        var stayingByPlayer = players.ToDictionary(
            p => p.PlayerId,
            p => ComputeStayingHexes(state, p)
        );

        // Step 1: Deduct build/gate costs
        ProcessBuildAndGateDeductions(state, players, events, map);

        // Step 2: Moves
        var combatHexes = ProcessMoves(state, players, events);

        // Step 3: Combat + undefended entry + gate cancellation check
        ProcessCombatAndEntry(state, players, events, combatHexes);
        ProcessGateOutcomes(state, players, events, map);

        // Step 4: Colonization
        ProcessColonization(state, players, events, combatHexes, map);

        // Step 5: Income (includes trade route computation)
        ProcessIncome(state, players, events, stayingByPlayer, map);

        // Step 6: Deploy newly built fleets
        ProcessFleetDeployment(state, players, events, map);

        state.ResolveEvents = events;

        // Win check
        if (CheckWinCondition(state, players, events))
        {
            state.Phase = NexusGamePhase.Ended;
            return;
        }

        // Advance to next round
        state.RoundNumber++;
        foreach (var player in players)
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
        List<NexusPlayerState> players,
        List<NexusResolveEvent> events,
        NexusMapLayout map
    )
    {
        var gateCost = GateCostForState(state);
        var resourceColors = map.ResourceColors;

        foreach (var player in players)
        {
            if (player.PendingBuildFleet)
            {
                var factionColor = player.Faction.ToColonyColor();
                player.DeductCredits(factionColor, BuildFleetCostPerColor);
                player.DeductCredits(NexusColonyColor.Gold, BuildFleetCostPerColor);
                player.PendingFleetDeployment = true;
            }

            if (player.PendingBeginNexusGate)
            {
                player.GateProgressBeforeThisTurn = player.GateProgress;
                var costDict = resourceColors.ToDictionary(c => c, _ => gateCost);
                foreach (var color in resourceColors)
                    player.DeductCredits(color, gateCost);

                if (player.GateProgress == NexusGateProgress.None)
                {
                    player.GateProgress = NexusGateProgress.Started;
                    events.Add(
                        new NexusGateBegunEvent(
                            player.PlayerId,
                            player.Faction,
                            map.NexusCoord,
                            costDict
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
                            map.NexusCoord,
                            costDict
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
        List<NexusPlayerState> players,
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
        foreach (var player in players)
        {
            foreach (var move in player.PendingFleetOrders.OfType<NexusMoveOrder>())
            {
                var fromState = GetHex(state, move.From);
                var toState = GetHex(state, move.To);

                fromState.RemoveFleets(player.Faction, move.Count);
                toState.AddFleets(player.Faction, move.Count);

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

        // Determine contested hexes (two or more factions present)
        var combatHexes = new HashSet<HexCoord>(
            state.Hexes.Where(h => h.Fleets.Count(kv => kv.Value > 0) >= 2).Select(h => h.Coord)
        );

        // Undefended entry: fleet moved into opponent-controlled hex with no opponent fleet
        foreach (var player in players)
        {
            foreach (var move in player.PendingFleetOrders.OfType<NexusMoveOrder>())
            {
                if (combatHexes.Contains(move.To))
                    continue; // will be handled by combat

                var hexState = GetHex(state, move.To);
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
        List<NexusPlayerState> players,
        List<NexusResolveEvent> events,
        HashSet<HexCoord> combatHexes
    )
    {
        foreach (var hex in combatHexes)
        {
            var hexState = GetHex(state, hex);
            var presentFactions = hexState.Fleets.Where(kv => kv.Value > 0).ToList();

            if (presentFactions.Count > 2)
            {
                // 3+ faction encounter: leave fleets, emit no combat event
                Trace.WriteLine(
                    $"[NexusGameEngine] 3+ faction encounter at {hex} \u2014 multi-faction combat not yet implemented."
                );
                continue;
            }

            var faction0 = presentFactions[0].Key;
            var faction1 = presentFactions[1].Key;
            var player0 = players.First(p => p.Faction == faction0);
            var player1 = players.First(p => p.Faction == faction1);
            var count0 = presentFactions[0].Value;
            var count1 = presentFactions[1].Value;

            Guid? winnerId;
            int losses0;
            int losses1;

            if (count0 > count1)
            {
                winnerId = player0.PlayerId;
                losses0 = count1;
                losses1 = count1;
            }
            else if (count1 > count0)
            {
                winnerId = player1.PlayerId;
                losses0 = count0;
                losses1 = count0;
            }
            else
            {
                winnerId = null;
                losses0 = count0;
                losses1 = count1;
            }

            events.Add(
                new NexusCombatEvent(
                    hex,
                    [
                        new NexusCombatParticipant(player0.PlayerId, faction0, count0, losses0),
                        new NexusCombatParticipant(player1.PlayerId, faction1, count1, losses1),
                    ],
                    winnerId
                )
            );

            if (winnerId == player0.PlayerId)
            {
                hexState.SetFleets(faction1, 0);
                hexState.RemoveFleets(faction0, losses0);
            }
            else if (winnerId == player1.PlayerId)
            {
                hexState.SetFleets(faction0, 0);
                hexState.RemoveFleets(faction1, losses1);
            }
            else
            {
                hexState.SetFleets(faction0, 0);
                hexState.SetFleets(faction1, 0);
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
        List<NexusPlayerState> players,
        List<NexusResolveEvent> events,
        NexusMapLayout map
    )
    {
        foreach (var player in players)
        {
            if (!player.PendingBeginNexusGate)
                continue;

            var nexusHex = GetHex(state, map.NexusCoord);
            var fleetOnNexus = nexusHex.GetFleets(player.Faction) > 0;

            if (!fleetOnNexus)
            {
                // Fleet destroyed — cancel gate
                player.GateProgress = player.GateProgressBeforeThisTurn;
                events.Add(
                    new NexusGateCancelledEvent(player.PlayerId, player.Faction, map.NexusCoord)
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
        List<NexusPlayerState> players,
        List<NexusResolveEvent> events,
        HashSet<HexCoord> combatHexes,
        NexusMapLayout map
    )
    {
        foreach (var player in players)
        {
            foreach (var order in player.PendingFleetOrders.OfType<NexusColonizeOrder>())
            {
                var hex = order.From;
                var hexState = GetHex(state, hex);
                var fleetCount = hexState.GetFleets(player.Faction);

                if (fleetCount == 0)
                    continue; // fleet destroyed in combat

                if (combatHexes.Contains(hex))
                {
                    events.Add(new NexusColonizeFailedEvent(player.PlayerId, player.Faction, hex));
                    continue;
                }

                hexState.ColonyOwnerId = player.PlayerId;
                var hexDef = map.ByCoord[hex];
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
        List<NexusPlayerState> players,
        List<NexusResolveEvent> events,
        Dictionary<Guid, HashSet<HexCoord>> stayingByPlayer,
        NexusMapLayout map
    )
    {
        // Filter staying hexes to those where the fleet survived combat
        var survivingByPlayer = stayingByPlayer.ToDictionary(
            kv => kv.Key,
            kv =>
                kv.Value.Where(h =>
                    {
                        var player = players.First(p => p.PlayerId == kv.Key);
                        return GetHex(state, h).GetFleets(player.Faction) > 0;
                    })
                    .ToHashSet()
        );

        var (newRoutes, newRouteKeys) = ComputeNewTradeRoutes(
            state,
            players,
            survivingByPlayer,
            map
        );
        EmitTradeRouteChangedEvents(state, newRoutes, newRouteKeys, events);
        state.ActiveTradeRoutes = newRoutes;

        var accumulated = AccumulateIncome(state, players, survivingByPlayer, map);
        EmitIncomeEvents(players, accumulated, events);

        foreach (var player in players)
        {
            if (!accumulated.TryGetValue(player.PlayerId, out var colorAmounts))
                continue;
            foreach (var (color, amount) in colorAmounts)
                player.AddCredits(color, amount);
        }
    }

    private static Dictionary<Guid, Dictionary<NexusColonyColor, int>> AccumulateIncome(
        NexusGameState state,
        List<NexusPlayerState> players,
        Dictionary<Guid, HashSet<HexCoord>> survivingByPlayer,
        NexusMapLayout map
    )
    {
        var acc = players.ToDictionary(
            p => p.PlayerId,
            _ => new Dictionary<NexusColonyColor, int>()
        );

        void AddIncome(Guid ownerId, NexusColonyColor color, int amount)
        {
            if (!acc.TryGetValue(ownerId, out var dict))
                return;
            dict[color] = dict.GetValueOrDefault(color) + amount;
        }

        // Hex base income
        foreach (var hexState in state.Hexes)
        {
            if (hexState.ColonyOwnerId is null)
                continue;
            var hexDef = map.ByCoord[hexState.Coord];
            if (hexDef.Color == NexusColonyColor.None)
                continue;

            // Faction-color hex: only earns income for the matching faction's owner
            if (hexDef.Color != NexusColonyColor.Gold)
            {
                var owner = players.FirstOrDefault(p => p.PlayerId == hexState.ColonyOwnerId);
                if (owner is null || owner.Faction.ToColonyColor() != hexDef.Color)
                    continue;
            }

            var amount = hexDef.IsHome ? 2 : 1;
            AddIncome(hexState.ColonyOwnerId.Value, hexDef.Color, amount);
        }

        // Trade route income
        foreach (var route in state.ActiveTradeRoutes)
        {
            var hex1Def = map.ByCoord[route.Hex1];
            var hex2Def = map.ByCoord[route.Hex2];
            var staying1 =
                survivingByPlayer.TryGetValue(route.Owner1, out var s1) && s1.Contains(route.Hex1);
            var staying2 =
                survivingByPlayer.TryGetValue(route.Owner2, out var s2) && s2.Contains(route.Hex2);
            AddIncome(route.Owner1, hex2Def.Color, staying1 ? 2 : 1);
            AddIncome(route.Owner2, hex1Def.Color, staying2 ? 2 : 1);
        }

        return acc;
    }

    private static void EmitIncomeEvents(
        List<NexusPlayerState> players,
        Dictionary<Guid, Dictionary<NexusColonyColor, int>> accumulated,
        List<NexusResolveEvent> events
    )
    {
        foreach (var player in players)
        {
            if (!accumulated.TryGetValue(player.PlayerId, out var colorAmounts))
                continue;
            if (colorAmounts.Values.Sum() == 0)
                continue;
            events.Add(new NexusIncomeEvent(player.PlayerId, player.Faction, colorAmounts));
        }
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
                var total = h.GetFleets(player.Faction);
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
        List<NexusPlayerState> players,
        Dictionary<Guid, HashSet<HexCoord>> survivingStaying,
        NexusMapLayout map
    )
    {
        var routes = new List<NexusTradeRoute>();
        var keys = new HashSet<(HexCoord, HexCoord)>();

        foreach (var player in players)
        {
            if (!survivingStaying.TryGetValue(player.PlayerId, out var staying))
                continue;
            foreach (var pos in staying)
                AddTradeRoutesForPosition(state, player, pos, routes, keys, map);
        }

        return (routes, keys);
    }

    private static void AddTradeRoutesForPosition(
        NexusGameState state,
        NexusPlayerState player,
        HexCoord pos,
        List<NexusTradeRoute> routes,
        HashSet<(HexCoord, HexCoord)> keys,
        NexusMapLayout map
    )
    {
        var hexState = GetHex(state, pos);
        if (hexState.ColonyOwnerId != player.PlayerId)
            return;

        foreach (var neighbour in pos.GetNeighbours())
        {
            if (!map.IsValidCoord(neighbour))
                continue;
            var neighbourHex = GetHex(state, neighbour);
            if (neighbourHex.ColonyOwnerId is null || neighbourHex.ColonyOwnerId == player.PlayerId)
                continue;

            var (h1, h2) = NormalizeRouteKey(pos, neighbour);
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

    private static (HexCoord, HexCoord) NormalizeRouteKey(HexCoord a, HexCoord b) =>
        a.Q < b.Q || (a.Q == b.Q && a.R < b.R) ? (a, b) : (b, a);

    private static void EmitTradeRouteChangedEvents(
        NexusGameState state,
        List<NexusTradeRoute> newRoutes,
        HashSet<(HexCoord, HexCoord)> newRouteKeys,
        List<NexusResolveEvent> events
    )
    {
        var previousKeys = state
            .ActiveTradeRoutes.Select(r => NormalizeRouteKey(r.Hex1, r.Hex2))
            .ToHashSet();

        foreach (var route in newRoutes)
        {
            var key = NormalizeRouteKey(route.Hex1, route.Hex2);
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
            var key = NormalizeRouteKey(old.Hex1, old.Hex2);
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

    // -------------------------------------------------------------------------
    // Step 6: Fleet deployment
    // -------------------------------------------------------------------------

    private static void ProcessFleetDeployment(
        NexusGameState state,
        List<NexusPlayerState> players,
        List<NexusResolveEvent> events,
        NexusMapLayout map
    )
    {
        foreach (var player in players)
        {
            if (!player.PendingFleetDeployment)
                continue;

            var homeCoord = map.GetHomeCoord(player.Faction);
            var homeHex = GetHex(state, homeCoord);
            homeHex.AddFleets(player.Faction, 1);

            events.Add(new NexusFleetDeployedEvent(player.PlayerId, player.Faction, homeCoord));
        }
    }

    // -------------------------------------------------------------------------
    // Win check
    // -------------------------------------------------------------------------

    private static bool CheckWinCondition(
        NexusGameState state,
        List<NexusPlayerState> players,
        List<NexusResolveEvent> events
    )
    {
        var completers = players
            .Where(p => p.GateProgress == NexusGateProgress.Completed && p.PendingBeginNexusGate)
            .ToList();

        if (completers.Count > 0)
        {
            if (completers.Count > 1)
            {
                events.Add(
                    new NexusDrawEvent("Multiple players completed the Nexus Gate simultaneously.")
                );
                state.Completion = new NexusGameCompletion(NexusGameOutcome.Draw, WinnerId: null);
            }
            else
            {
                var winner = completers[0];
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
            var systemsByPlayer = players
                .Select(p =>
                    (Player: p, Systems: state.Hexes.Count(h => h.ColonyOwnerId == p.PlayerId))
                )
                .OrderByDescending(x => x.Systems)
                .ToList();

            var topSystems = systemsByPlayer[0].Systems;
            var topPlayers = systemsByPlayer.Where(x => x.Systems == topSystems).ToList();

            if (topPlayers.Count == 1)
            {
                var winner = topPlayers[0].Player;
                var runnerUpSystems = systemsByPlayer[1].Systems;
                events.Add(
                    new NexusTiebreakerVictoryEvent(
                        winner.PlayerId,
                        winner.Faction,
                        topSystems,
                        runnerUpSystems
                    )
                );
                state.Completion = new NexusGameCompletion(
                    NexusGameOutcome.Victory,
                    winner.PlayerId
                );
            }
            else
            {
                events.Add(new NexusTiebreakerDrawEvent(topSystems));
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
        var map = GetMap(state);
        return map
            .Hexes.Select(def =>
            {
                var hexState = GetHex(state, def.Coord);
                NexusFactionColor? ownerFaction = hexState.ColonyOwnerId is { } ownerId
                    ? GetFactionForPlayer(state, ownerId)
                    : null;

                var fleetCounts = ImmutableDictionary.CreateRange(
                    state.Players.Select(p =>
                        KeyValuePair.Create(p.Faction, hexState.GetFleets(p.Faction))
                    )
                );

                return new NexusHexView(
                    def.Coord,
                    def.Color,
                    def.IsNexus,
                    def.IsHome,
                    hexState.ColonyOwnerId,
                    ownerFaction,
                    fleetCounts
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
                ImmutableDictionary<NexusColonyColor, int>.Empty,
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
            player.Credits.ToImmutableDictionary(),
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
        state.Players.FirstOrDefault(p => p.PlayerId == playerId);

    private static NexusFactionColor GetFactionForPlayer(NexusGameState state, Guid playerId) =>
        state.Players.First(p => p.PlayerId == playerId).Faction;

    private static NexusHexState GetHex(NexusGameState state, HexCoord coord) =>
        state.Hexes.First(h => h.Coord == coord);

    private static Guid? GetOwnerOfHex(NexusGameState state, HexCoord coord) =>
        GetHex(state, coord).ColonyOwnerId;
}
