namespace Spx.Nexus.Domain;

/// <summary>
/// Combat rules for Nexus Protocol.
/// <para>Hit thresholds are resolved from each unit's <see cref="NexusUnitProfile"/> threshold
/// dictionaries using a four-step priority chain: phase+unit, phase+category, any+unit,
/// any+category. A <c>null</c> dictionary value explicitly forbids the matchup.
/// Phase participation and targetability are derived from <see cref="NexusPhaseParticipation"/> flags.</para>
/// </summary>
public static class NexusCombatSpec
{
    private const int MinimumHitThreshold = 2;

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
        var targetCategory = target.Profile().Category;

        // Priority: specific phase beats any-phase; unit beats category at the same specificity.
        if (profile.UnitThresholds.TryGetValue((phase, target), out var ut1))
            return ut1.HasValue ? Math.Max(MinimumHitThreshold, ut1.Value) : null;

        if (profile.CategoryThresholds.TryGetValue((phase, targetCategory), out var ct1))
            return ct1.HasValue ? Math.Max(MinimumHitThreshold, ct1.Value) : null;

        if (profile.UnitThresholds.TryGetValue((null, target), out var ut2))
            return ut2.HasValue ? Math.Max(MinimumHitThreshold, ut2.Value) : null;

        if (profile.CategoryThresholds.TryGetValue((null, targetCategory), out var ct2))
            return ct2.HasValue ? Math.Max(MinimumHitThreshold, ct2.Value) : null;

        return null;
    }

    /// <summary>Returns <c>true</c> if <paramref name="unit"/> rolls dice as an attacker in <paramref name="phase"/>.</summary>
    public static bool CanAttack(NexusUnitType unit, CombatPhase phase) =>
        unit.Profile().AttacksIn.HasFlag(ToPhaseFlag(phase));

    /// <summary>
    /// Returns <c>true</c> if <paramref name="unit"/> can be selected as a combat target in <paramref name="phase"/>.
    /// Derived from the unit's <see cref="NexusUnitProfile.DefendsIn"/> bitmask.
    /// </summary>
    public static bool IsTargetable(NexusUnitType unit, CombatPhase phase) =>
        unit.Profile().DefendsIn.HasFlag(ToPhaseFlag(phase));

    private static NexusPhaseParticipation ToPhaseFlag(CombatPhase phase) =>
        phase switch
        {
            CombatPhase.Intercept => NexusPhaseParticipation.Intercept,
            CombatPhase.Line => NexusPhaseParticipation.Line,
            CombatPhase.Orbit => NexusPhaseParticipation.Orbit,
            CombatPhase.Surface => NexusPhaseParticipation.Surface,
            _ => NexusPhaseParticipation.None,
        };
}
