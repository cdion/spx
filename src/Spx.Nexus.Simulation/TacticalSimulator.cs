using System.Collections.Immutable;
using Spx.Nexus.Domain;

namespace Spx.Nexus.Simulation;

public sealed class TacticalSimulator
{
    private static readonly Guid AttackerId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid DefenderId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    public static TacticalReportData Run(TacticalSimulationSettings settings)
    {
        var profiles = TacticalProfileLibrary.CreateProfiles();
        var scenarios = TacticalProfileLibrary.CreateScenarios(profiles);

        return Run(settings, scenarios, profiles);
    }

    public static TacticalReportData Run(
        TacticalSimulationSettings settings,
        IReadOnlyList<TacticalScenario> scenarios,
        IReadOnlyList<TacticalProfile> profiles
    )
    {
        var matchupSummaries = new List<TacticalMatchupSummary>();
        var phaseSummaries = new List<TacticalPhaseSummary>();
        var survivorSummaries = new List<TacticalSurvivorSummary>();

        foreach (var scenario in scenarios)
        {
            var scenarioProfileIds = scenario.ProfileIds.ToHashSet(StringComparer.Ordinal);
            var scenarioProfiles = profiles
                .Where(profile => scenarioProfileIds.Contains(profile.Id))
                .ToArray();

            foreach (var attacker in scenarioProfiles)
            {
                foreach (var defender in scenarioProfiles)
                {
                    var aggregate = RunMatchup(settings, scenario, attacker, defender);
                    matchupSummaries.Add(aggregate.Matchup);
                    phaseSummaries.AddRange(aggregate.Phases);
                    survivorSummaries.AddRange(aggregate.Survivors);
                }
            }
        }

        return new TacticalReportData(
            DateTimeOffset.UtcNow,
            settings,
            scenarios
                .Select(s => new TacticalScenarioSummary(
                    s.Id,
                    s.Label,
                    s.InitialControlOwner,
                    s.System,
                    s.MaxRounds,
                    s.ProfileIds
                ))
                .ToArray(),
            profiles
                .Select(p => new TacticalProfileSummary(
                    p.Id,
                    p.Label,
                    p.Tags,
                    p.TotalCost,
                    p.Units
                ))
                .ToArray(),
            matchupSummaries,
            phaseSummaries,
            survivorSummaries
        );
    }

    private static MatchupAggregateResult RunMatchup(
        TacticalSimulationSettings settings,
        TacticalScenario scenario,
        TacticalProfile attacker,
        TacticalProfile defender
    )
    {
        var attackerWins = 0;
        var defenderWins = 0;
        var contested = 0;
        var mutualDestruction = 0;
        var attackerControl = 0;
        var defenderControl = 0;
        var attackerSurvivorCost = 0.0;
        var defenderSurvivorCost = 0.0;
        var firstContactActivity = 0;
        var totalTrials = 0;
        var phaseBuckets = Enum.GetValues<CombatPhase>()
            .ToDictionary(phase => phase, _ => new PhaseAccumulator());
        var attackerSurvivors = new Dictionary<NexusUnitType, double>();
        var defenderSurvivors = new Dictionary<NexusUnitType, double>();

        for (var iteration = 0; iteration < settings.IterationsPerMatchup; iteration++)
        {
            var seed = settings.BaseSeed + iteration;
            foreach (var attackerUsesPrimarySlot in new[] { true, false })
            {
                var outcome = RunTrial(seed, scenario, attacker, defender, attackerUsesPrimarySlot);
                totalTrials++;

                attackerWins += outcome.AttackerWon ? 1 : 0;
                defenderWins += outcome.DefenderWon ? 1 : 0;
                contested += outcome.Contested ? 1 : 0;
                mutualDestruction += outcome.MutualDestruction ? 1 : 0;
                attackerControl += outcome.AttackerControlled ? 1 : 0;
                defenderControl += outcome.DefenderControlled ? 1 : 0;
                attackerSurvivorCost += outcome.AttackerSurvivorCost;
                defenderSurvivorCost += outcome.DefenderSurvivorCost;
                firstContactActivity += outcome.FirstContactActive ? 1 : 0;

                foreach (var phase in outcome.Phases)
                {
                    var bucket = phaseBuckets[phase.Phase];
                    bucket.AttackerAttacks += phase.AttackerAttacks;
                    bucket.AttackerHits += phase.AttackerHits;
                    bucket.AttackerKills += phase.AttackerKills;
                    bucket.DefenderAttacks += phase.DefenderAttacks;
                    bucket.DefenderHits += phase.DefenderHits;
                    bucket.DefenderKills += phase.DefenderKills;
                }

                AccumulateUnits(attackerSurvivors, outcome.AttackerSurvivors);
                AccumulateUnits(defenderSurvivors, outcome.DefenderSurvivors);
            }
        }

        var dominantKillPhase = phaseBuckets
            .OrderByDescending(pair => pair.Value.AttackerKills + pair.Value.DefenderKills)
            .First()
            .Key;

        var matchup = new TacticalMatchupSummary(
            scenario.Id,
            attacker.Id,
            defender.Id,
            totalTrials,
            (double)firstContactActivity / totalTrials,
            (double)attackerWins / totalTrials,
            (double)defenderWins / totalTrials,
            (double)contested / totalTrials,
            (double)mutualDestruction / totalTrials,
            (double)attackerControl / totalTrials,
            (double)defenderControl / totalTrials,
            attackerSurvivorCost / totalTrials,
            defenderSurvivorCost / totalTrials,
            dominantKillPhase.ToString()
        );

        var phases = phaseBuckets
            .OrderBy(pair => pair.Key)
            .SelectMany(pair =>
                new[]
                {
                    new TacticalPhaseSummary(
                        scenario.Id,
                        attacker.Id,
                        defender.Id,
                        pair.Key.ToString(),
                        "attacker",
                        pair.Value.AttackerAttacks / totalTrials,
                        pair.Value.AttackerHits / totalTrials,
                        pair.Value.AttackerKills / totalTrials
                    ),
                    new TacticalPhaseSummary(
                        scenario.Id,
                        attacker.Id,
                        defender.Id,
                        pair.Key.ToString(),
                        "defender",
                        pair.Value.DefenderAttacks / totalTrials,
                        pair.Value.DefenderHits / totalTrials,
                        pair.Value.DefenderKills / totalTrials
                    ),
                }
            )
            .ToArray();

        var survivors = attackerSurvivors
            .Select(pair => new TacticalSurvivorSummary(
                scenario.Id,
                attacker.Id,
                defender.Id,
                "attacker",
                pair.Key,
                pair.Value / totalTrials
            ))
            .Concat(
                defenderSurvivors.Select(pair => new TacticalSurvivorSummary(
                    scenario.Id,
                    attacker.Id,
                    defender.Id,
                    "defender",
                    pair.Key,
                    pair.Value / totalTrials
                ))
            )
            .OrderBy(summary => summary.Side)
            .ThenBy(summary => summary.UnitType)
            .ToArray();

        return new MatchupAggregateResult(matchup, phases, survivors);
    }

    private static TrialOutcome RunTrial(
        int seed,
        TacticalScenario scenario,
        TacticalProfile attacker,
        TacticalProfile defender,
        bool attackerUsesPrimarySlot
    )
    {
        var attackerPlayerId = attackerUsesPrimarySlot ? AttackerId : DefenderId;
        var defenderPlayerId = attackerUsesPrimarySlot ? DefenderId : AttackerId;
        var state = new NexusState();
        NexusEngine.Initialize(
            state,
            new InitializeNexusGameCommand(
                ImmutableArray.Create(
                    new NexusSessionPlayer(AttackerId),
                    new NexusSessionPlayer(DefenderId)
                )
            ),
            CreateRandom(seed, 0, attackerUsesPrimarySlot ? 1 : 2)
        );

        foreach (var system in state.Systems)
            system.Units.Clear();

        var battleSystem = state.Systems.First(system => system.Coord == scenario.System);
        battleSystem.ControlOwner = scenario.InitialControlOwner switch
        {
            TacticalControlOwner.Attacker => attackerPlayerId,
            TacticalControlOwner.Defender => defenderPlayerId,
            _ => null,
        };

        AddProfileUnits(battleSystem, attackerPlayerId, attacker);
        AddProfileUnits(battleSystem, defenderPlayerId, defender);

        var phaseOutcomes = new List<TrialPhaseOutcome>();
        Dictionary<NexusUnitType, int> attackerSurvivors = [];
        Dictionary<NexusUnitType, int> defenderSurvivors = [];
        var attackerHasUnits = true;
        var defenderHasUnits = true;
        var firstContactActive = false;

        for (var round = 1; round <= scenario.MaxRounds; round++)
        {
            SubmitEmptyOrders(state, AttackerId, seed, round, 0);
            SubmitEmptyOrders(state, DefenderId, seed, round, 1);

            var phaseOutcomesForRound = BuildPhaseOutcomes(
                state.LastResolveEvents,
                attackerPlayerId,
                defenderPlayerId
            );

            if (round == 1)
            {
                firstContactActive = phaseOutcomesForRound.Any(phase =>
                    phase.AttackerHits > 0 || phase.DefenderHits > 0
                );
            }

            phaseOutcomes.AddRange(phaseOutcomesForRound);
            attackerSurvivors = battleSystem.GetPlayerUnits(attackerPlayerId);
            defenderSurvivors = battleSystem.GetPlayerUnits(defenderPlayerId);
            attackerHasUnits = attackerSurvivors.Count > 0;
            defenderHasUnits = defenderSurvivors.Count > 0;

            if (!attackerHasUnits || !defenderHasUnits)
                break;
        }

        return new TrialOutcome(
            !defenderHasUnits && attackerHasUnits,
            !attackerHasUnits && defenderHasUnits,
            attackerHasUnits && defenderHasUnits,
            !attackerHasUnits && !defenderHasUnits,
            battleSystem.ControlOwner == attackerPlayerId,
            battleSystem.ControlOwner == defenderPlayerId,
            ComputeSurvivorCost(attackerSurvivors),
            ComputeSurvivorCost(defenderSurvivors),
            firstContactActive,
            attackerSurvivors,
            defenderSurvivors,
            phaseOutcomes.ToArray()
        );
    }

    private static void SubmitEmptyOrders(
        NexusState state,
        Guid playerId,
        int seed,
        int round,
        int orderIndex
    )
    {
        var result = NexusEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(playerId, state.RoundNumber, [], [], false),
            CreateRandom(seed, round, orderIndex, playerId == AttackerId ? 1 : 2)
        );

        if (result is NexusTurnOrdersRejected rejected)
            throw new InvalidOperationException(rejected.ErrorMessage);
    }

    private static Random CreateRandom(int seed, params int[] salts)
    {
        var mixed = seed;
        foreach (var salt in salts)
            mixed = unchecked((mixed * 397) ^ salt);

        return new Random(mixed);
    }

    private static TrialPhaseOutcome[] BuildPhaseOutcomes(
        IReadOnlyList<NexusResolveEvent> events,
        Guid attackerPlayerId,
        Guid defenderPlayerId
    )
    {
        var phases = Enum.GetValues<CombatPhase>()
            .ToDictionary(phase => phase, _ => new PhaseAccumulator());

        foreach (var phaseResult in events.OfType<NexusPhaseResultEvent>())
        {
            var bucket = phases[phaseResult.Phase];
            bucket.AttackerAttacks += phaseResult.AttackRolls.Count(roll =>
                roll.AttackingPlayerId == attackerPlayerId
            );
            bucket.AttackerHits += phaseResult.AttackRolls.Count(roll =>
                roll.AttackingPlayerId == attackerPlayerId && roll.IsHit
            );
            bucket.AttackerKills += phaseResult
                .Losses.Where(loss => loss.PlayerId == defenderPlayerId)
                .Sum(loss => loss.Count);
            bucket.DefenderAttacks += phaseResult.AttackRolls.Count(roll =>
                roll.AttackingPlayerId == defenderPlayerId
            );
            bucket.DefenderHits += phaseResult.AttackRolls.Count(roll =>
                roll.AttackingPlayerId == defenderPlayerId && roll.IsHit
            );
            bucket.DefenderKills += phaseResult
                .Losses.Where(loss => loss.PlayerId == attackerPlayerId)
                .Sum(loss => loss.Count);
        }

        return phases
            .OrderBy(pair => pair.Key)
            .Select(pair => new TrialPhaseOutcome(
                pair.Key,
                pair.Value.AttackerAttacks,
                pair.Value.AttackerHits,
                pair.Value.AttackerKills,
                pair.Value.DefenderAttacks,
                pair.Value.DefenderHits,
                pair.Value.DefenderKills
            ))
            .ToArray();
    }

    private static void AddProfileUnits(
        NexusSystemState battleSystem,
        Guid playerId,
        TacticalProfile profile
    )
    {
        foreach (var unit in profile.Units)
            battleSystem.AddUnits(playerId, unit.UnitType, unit.Count, unit.RemainingHull);
    }

    private static double ComputeSurvivorCost(IReadOnlyDictionary<NexusUnitType, int> survivors) =>
        survivors.Sum(pair => pair.Key.Cost() * pair.Value);

    private static void AccumulateUnits(
        Dictionary<NexusUnitType, double> totals,
        IReadOnlyDictionary<NexusUnitType, int> survivors
    )
    {
        foreach (var pair in survivors)
        {
            totals.TryGetValue(pair.Key, out var currentTotal);
            totals[pair.Key] = currentTotal + pair.Value;
        }
    }

    private sealed class PhaseAccumulator
    {
        public double AttackerAttacks { get; set; }

        public double AttackerHits { get; set; }

        public double AttackerKills { get; set; }

        public double DefenderAttacks { get; set; }

        public double DefenderHits { get; set; }

        public double DefenderKills { get; set; }
    }

    private sealed record TrialPhaseOutcome(
        CombatPhase Phase,
        double AttackerAttacks,
        double AttackerHits,
        double AttackerKills,
        double DefenderAttacks,
        double DefenderHits,
        double DefenderKills
    );

    private sealed record TrialOutcome(
        bool AttackerWon,
        bool DefenderWon,
        bool Contested,
        bool MutualDestruction,
        bool AttackerControlled,
        bool DefenderControlled,
        double AttackerSurvivorCost,
        double DefenderSurvivorCost,
        bool FirstContactActive,
        IReadOnlyDictionary<NexusUnitType, int> AttackerSurvivors,
        IReadOnlyDictionary<NexusUnitType, int> DefenderSurvivors,
        IReadOnlyList<TrialPhaseOutcome> Phases
    );

    private sealed record MatchupAggregateResult(
        TacticalMatchupSummary Matchup,
        IReadOnlyList<TacticalPhaseSummary> Phases,
        IReadOnlyList<TacticalSurvivorSummary> Survivors
    );
}
