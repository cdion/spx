namespace Spx.Nexus.Domain;

/// <summary>
/// Combat rules for Nexus Protocol.
/// <para>Hit thresholds are computed from each unit's <see cref="NexusUnitProfile"/> plus a small
/// per-matchup exception table. Phase participation and targetability are derived from
/// <see cref="NexusPhaseParticipation"/> flags and <see cref="NexusUnitCategory"/> respectively.</para>
/// </summary>
public static class NexusCombatSpec
{
    private const int MinimumHitThreshold = 2;

    /// <summary>
    /// Per-matchup threshold overrides applied on top of the profile's base category threshold.
    /// Entries may be phase-specific or phase-agnostic when the same override should apply in
    /// every phase where the attacker can target that unit.
    /// </summary>
    private static readonly Dictionary<
        (NexusUnitType Attacker, CombatPhase? Phase, NexusUnitType Target),
        int
    > Exceptions = new()
    {
        [(NexusUnitType.Interceptor, null, NexusUnitType.Bomber)] = 2, // specialist anti-bomber
        [(NexusUnitType.Interceptor, null, NexusUnitType.Fighter)] = 5, // softer anti-fighter edge
        [(NexusUnitType.Bomber, null, NexusUnitType.Interceptor)] = 6, // poor anti-interceptor
        [(NexusUnitType.Destroyer, CombatPhase.Engage, NexusUnitType.Interceptor)] = 6,
        [(NexusUnitType.Destroyer, CombatPhase.Engage, NexusUnitType.Fighter)] = 6,
        [(NexusUnitType.Destroyer, CombatPhase.Engage, NexusUnitType.Bomber)] = 6,
        [(NexusUnitType.Infantry, null, NexusUnitType.Armor)] = 5, // infantry struggles vs armor
        [(NexusUnitType.Armor, null, NexusUnitType.Armor)] = 4, // armor vs armor is harder
    };

    /// <summary>
    /// Returns the minimum d6 roll needed for <paramref name="attacker"/> to score a hit on
    /// <paramref name="target"/> during <paramref name="phase"/>, or <c>null</c> if that
    /// attacker cannot target that unit type in that phase.
    /// </summary>
    public static int? GetHitThreshold(
        NexusUnitType attacker,
        CombatPhase phase,
        NexusUnitType target
    )
    {
        if (!CanAttack(attacker, phase) || !IsTargetable(target, phase))
            return null;

        var profile = attacker.Profile();
        var baseThreshold = target.Category() switch
        {
            NexusUnitCategory.Strike => profile.StrikeThreshold,
            NexusUnitCategory.Capital => profile.CapitalThreshold,
            NexusUnitCategory.Planetary => profile.PlanetaryThreshold,
            _ => null,
        };

        if (baseThreshold is null)
            return null;

        var effectiveThreshold = baseThreshold.Value;

        if (Exceptions.TryGetValue((attacker, phase, target), out var phaseOverride))
            effectiveThreshold = phaseOverride;
        else if (Exceptions.TryGetValue((attacker, null, target), out var genericOverride))
            effectiveThreshold = genericOverride;

        return Math.Max(MinimumHitThreshold, effectiveThreshold - 1);
    }

    /// <summary>Returns <c>true</c> if <paramref name="unit"/> rolls dice as an attacker in <paramref name="phase"/>.</summary>
    public static bool CanAttack(NexusUnitType unit, CombatPhase phase) =>
        unit.Profile().AttacksIn.HasFlag(ToPhaseFlag(phase));

    /// <summary>
    /// Returns <c>true</c> if <paramref name="unit"/> can be selected as a combat target in <paramref name="phase"/>.
    /// Derived from unit category — Capital units are only targetable in Engage; Strike units in Screen and Engage;
    /// Planetary units in Bombard and Assault.
    /// </summary>
    public static bool IsTargetable(NexusUnitType unit, CombatPhase phase) =>
        (unit.Category(), phase) switch
        {
            (NexusUnitCategory.Strike, CombatPhase.Screen) => true,
            (NexusUnitCategory.Strike, CombatPhase.Engage) => true,
            (NexusUnitCategory.Capital, CombatPhase.Engage) => true,
            (NexusUnitCategory.Planetary, CombatPhase.Bombard) => true,
            (NexusUnitCategory.Planetary, CombatPhase.Assault) => true,
            _ => false,
        };

    private static NexusPhaseParticipation ToPhaseFlag(CombatPhase phase) =>
        phase switch
        {
            CombatPhase.Screen => NexusPhaseParticipation.Screen,
            CombatPhase.Engage => NexusPhaseParticipation.Engage,
            CombatPhase.Bombard => NexusPhaseParticipation.Bombard,
            CombatPhase.Assault => NexusPhaseParticipation.Assault,
            _ => NexusPhaseParticipation.None,
        };
}
