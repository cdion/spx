namespace Spx.Game.Domain;

/// <summary>
/// Static lookup tables for the Nexus Protocol combat rules.
/// Encodes which units can attack/be targeted in each combat phase,
/// and the hit threshold (roll needed on a d6) for each attacker-target pair.
/// </summary>
public static class NexusCombatSpec
{
    public const int PhaseSquadron = 1;
    public const int PhaseNaval = 2;
    public const int PhaseBombardment = 3;
    public const int PhaseGround = 4;

    /// <summary>
    /// Returns the minimum d6 roll needed for <paramref name="attacker"/> to score a hit on
    /// <paramref name="target"/> during <paramref name="phase"/>, or <c>null</c> if that
    /// attacker cannot target that unit type in that phase.
    /// </summary>
    public static int? GetHitThreshold(NexusUnitType attacker, int phase, NexusUnitType target) =>
        (attacker, phase, target) switch
        {
            // ── Phase 1: Squadron Combat ─────────────────────────────────────────
            // Fighter attacks all squadrons at 4+
            (NexusUnitType.Fighter, PhaseSquadron, NexusUnitType.Interceptor) => 4,
            (NexusUnitType.Fighter, PhaseSquadron, NexusUnitType.Fighter) => 4,
            (NexusUnitType.Fighter, PhaseSquadron, NexusUnitType.Bomber) => 4,

            // Interceptor: 2+ vs Bomber, 4+ vs Fighter/Interceptor
            (NexusUnitType.Interceptor, PhaseSquadron, NexusUnitType.Bomber) => 2,
            (NexusUnitType.Interceptor, PhaseSquadron, NexusUnitType.Fighter) => 4,
            (NexusUnitType.Interceptor, PhaseSquadron, NexusUnitType.Interceptor) => 4,

            // Bomber: 5+ vs Fighter/Bomber, 6+ vs Interceptor
            (NexusUnitType.Bomber, PhaseSquadron, NexusUnitType.Fighter) => 5,
            (NexusUnitType.Bomber, PhaseSquadron, NexusUnitType.Bomber) => 5,
            (NexusUnitType.Bomber, PhaseSquadron, NexusUnitType.Interceptor) => 6,

            // Destroyer attacks all squadrons at 3+ (not itself targetable in P1)
            (NexusUnitType.Destroyer, PhaseSquadron, NexusUnitType.Interceptor) => 3,
            (NexusUnitType.Destroyer, PhaseSquadron, NexusUnitType.Fighter) => 3,
            (NexusUnitType.Destroyer, PhaseSquadron, NexusUnitType.Bomber) => 3,

            // ── Phase 2: Naval Combat ────────────────────────────────────────────
            // Fighter: 6+ vs ships (does not attack squadrons in P2)
            (NexusUnitType.Fighter, PhaseNaval, NexusUnitType.Frigate) => 6,
            (NexusUnitType.Fighter, PhaseNaval, NexusUnitType.Destroyer) => 6,
            (NexusUnitType.Fighter, PhaseNaval, NexusUnitType.Cruiser) => 6,
            (NexusUnitType.Fighter, PhaseNaval, NexusUnitType.Carrier) => 6,

            // Bomber: 4+ vs ships (does not attack squadrons in P2)
            (NexusUnitType.Bomber, PhaseNaval, NexusUnitType.Frigate) => 4,
            (NexusUnitType.Bomber, PhaseNaval, NexusUnitType.Destroyer) => 4,
            (NexusUnitType.Bomber, PhaseNaval, NexusUnitType.Cruiser) => 4,
            (NexusUnitType.Bomber, PhaseNaval, NexusUnitType.Carrier) => 4,

            // Destroyer: 3+ vs squadrons, 5+ vs ships
            (NexusUnitType.Destroyer, PhaseNaval, NexusUnitType.Interceptor) => 3,
            (NexusUnitType.Destroyer, PhaseNaval, NexusUnitType.Fighter) => 3,
            (NexusUnitType.Destroyer, PhaseNaval, NexusUnitType.Bomber) => 3,
            (NexusUnitType.Destroyer, PhaseNaval, NexusUnitType.Frigate) => 5,
            (NexusUnitType.Destroyer, PhaseNaval, NexusUnitType.Destroyer) => 5,
            (NexusUnitType.Destroyer, PhaseNaval, NexusUnitType.Cruiser) => 5,
            (NexusUnitType.Destroyer, PhaseNaval, NexusUnitType.Carrier) => 5,

            // Frigate: 5+ vs squadrons, 4+ vs ships
            (NexusUnitType.Frigate, PhaseNaval, NexusUnitType.Interceptor) => 5,
            (NexusUnitType.Frigate, PhaseNaval, NexusUnitType.Fighter) => 5,
            (NexusUnitType.Frigate, PhaseNaval, NexusUnitType.Bomber) => 5,
            (NexusUnitType.Frigate, PhaseNaval, NexusUnitType.Frigate) => 4,
            (NexusUnitType.Frigate, PhaseNaval, NexusUnitType.Destroyer) => 4,
            (NexusUnitType.Frigate, PhaseNaval, NexusUnitType.Cruiser) => 4,
            (NexusUnitType.Frigate, PhaseNaval, NexusUnitType.Carrier) => 4,

            // Cruiser: 6+ vs squadrons, 3+ vs ships
            (NexusUnitType.Cruiser, PhaseNaval, NexusUnitType.Interceptor) => 6,
            (NexusUnitType.Cruiser, PhaseNaval, NexusUnitType.Fighter) => 6,
            (NexusUnitType.Cruiser, PhaseNaval, NexusUnitType.Bomber) => 6,
            (NexusUnitType.Cruiser, PhaseNaval, NexusUnitType.Frigate) => 3,
            (NexusUnitType.Cruiser, PhaseNaval, NexusUnitType.Destroyer) => 3,
            (NexusUnitType.Cruiser, PhaseNaval, NexusUnitType.Cruiser) => 3,
            (NexusUnitType.Cruiser, PhaseNaval, NexusUnitType.Carrier) => 3,

            // Carrier: 6+ vs squadrons, 6+ vs ships
            (NexusUnitType.Carrier, PhaseNaval, NexusUnitType.Interceptor) => 6,
            (NexusUnitType.Carrier, PhaseNaval, NexusUnitType.Fighter) => 6,
            (NexusUnitType.Carrier, PhaseNaval, NexusUnitType.Bomber) => 6,
            (NexusUnitType.Carrier, PhaseNaval, NexusUnitType.Frigate) => 6,
            (NexusUnitType.Carrier, PhaseNaval, NexusUnitType.Destroyer) => 6,
            (NexusUnitType.Carrier, PhaseNaval, NexusUnitType.Cruiser) => 6,
            (NexusUnitType.Carrier, PhaseNaval, NexusUnitType.Carrier) => 6,

            // ── Phase 3: Bombardment ─────────────────────────────────────────────
            // Bomber: 4+ vs ground forces
            (NexusUnitType.Bomber, PhaseBombardment, NexusUnitType.Infantry) => 4,
            (NexusUnitType.Bomber, PhaseBombardment, NexusUnitType.Armor) => 4,

            // Cruiser: 6+ vs ground forces
            (NexusUnitType.Cruiser, PhaseBombardment, NexusUnitType.Infantry) => 6,
            (NexusUnitType.Cruiser, PhaseBombardment, NexusUnitType.Armor) => 6,

            // ── Phase 4: Ground Combat ───────────────────────────────────────────
            // Infantry: 4+ vs Infantry, 5+ vs Armor
            (NexusUnitType.Infantry, PhaseGround, NexusUnitType.Infantry) => 4,
            (NexusUnitType.Infantry, PhaseGround, NexusUnitType.Armor) => 5,

            // Armor: 3+ vs Infantry, 4+ vs Armor
            (NexusUnitType.Armor, PhaseGround, NexusUnitType.Infantry) => 3,
            (NexusUnitType.Armor, PhaseGround, NexusUnitType.Armor) => 4,

            _ => null,
        };

    /// <summary>Returns <c>true</c> if <paramref name="unit"/> rolls dice as an attacker in <paramref name="phase"/>.</summary>
    public static bool CanAttack(NexusUnitType unit, int phase) =>
        phase switch
        {
            PhaseSquadron => unit
                is NexusUnitType.Interceptor
                    or NexusUnitType.Fighter
                    or NexusUnitType.Bomber
                    or NexusUnitType.Destroyer,
            PhaseNaval => unit
                is NexusUnitType.Fighter
                    or NexusUnitType.Bomber
                    or NexusUnitType.Destroyer
                    or NexusUnitType.Frigate
                    or NexusUnitType.Cruiser
                    or NexusUnitType.Carrier,
            PhaseBombardment => unit is NexusUnitType.Bomber or NexusUnitType.Cruiser,
            PhaseGround => unit is NexusUnitType.Infantry or NexusUnitType.Armor,
            _ => false,
        };

    /// <summary>Returns <c>true</c> if <paramref name="unit"/> can be selected as a combat target in <paramref name="phase"/>.</summary>
    public static bool IsTargetable(NexusUnitType unit, int phase) =>
        phase switch
        {
            PhaseSquadron => unit
                is NexusUnitType.Interceptor
                    or NexusUnitType.Fighter
                    or NexusUnitType.Bomber,
            PhaseNaval => unit
                is NexusUnitType.Interceptor
                    or NexusUnitType.Fighter
                    or NexusUnitType.Bomber
                    or NexusUnitType.Destroyer
                    or NexusUnitType.Frigate
                    or NexusUnitType.Cruiser
                    or NexusUnitType.Carrier,
            PhaseBombardment => unit is NexusUnitType.Infantry or NexusUnitType.Armor,
            PhaseGround => unit is NexusUnitType.Infantry or NexusUnitType.Armor,
            _ => false,
        };
}
