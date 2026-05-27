namespace Spx.Game.Domain;

/// <summary>
/// Combat rules for Nexus Protocol.
/// <para>Hit thresholds are computed from each unit's <see cref="NexusUnitProfile"/> plus a small
/// per-matchup exception table. Phase participation and targetability are derived from
/// <see cref="NexusPhaseParticipation"/> flags and <see cref="NexusUnitCategory"/> respectively.</para>
/// </summary>
public static class NexusCombatSpec
{
    public const int PhaseScreen = 1;
    public const int PhaseEngage = 2;
    public const int PhaseBombard = 3;
    public const int PhaseAssault = 4;

    /// <summary>
    /// Per-matchup threshold overrides applied on top of the profile's base category threshold.
    /// Covers asymmetric specialist matchups that the base profile cannot express with one number.
    /// </summary>
    private static readonly Dictionary<
        (NexusUnitType Attacker, NexusUnitType Target),
        int
    > Exceptions = new()
    {
        [(NexusUnitType.Interceptor, NexusUnitType.Bomber)] = 2, // specialist anti-bomber
        [(NexusUnitType.Bomber, NexusUnitType.Interceptor)] = 6, // poor anti-interceptor
        [(NexusUnitType.Infantry, NexusUnitType.Armor)] = 5, // infantry struggles vs armor
        [(NexusUnitType.Armor, NexusUnitType.Armor)] = 4, // armor vs armor is harder
    };

    /// <summary>
    /// Returns the minimum d6 roll needed for <paramref name="attacker"/> to score a hit on
    /// <paramref name="target"/> during <paramref name="phase"/>, or <c>null</c> if that
    /// attacker cannot target that unit type in that phase.
    /// </summary>
    public static int? GetHitThreshold(NexusUnitType attacker, int phase, NexusUnitType target)
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

        return Exceptions.TryGetValue((attacker, target), out var ovr) ? ovr : baseThreshold;
    }

    /// <summary>Returns <c>true</c> if <paramref name="unit"/> rolls dice as an attacker in <paramref name="phase"/>.</summary>
    public static bool CanAttack(NexusUnitType unit, int phase) =>
        unit.Profile().AttacksIn.HasFlag(ToPhaseFlag(phase));

    /// <summary>
    /// Returns <c>true</c> if <paramref name="unit"/> can be selected as a combat target in <paramref name="phase"/>.
    /// Derived from unit category — Capital units are only targetable in Engage; Strike units in Screen and Engage;
    /// Planetary units in Bombard and Assault.
    /// </summary>
    public static bool IsTargetable(NexusUnitType unit, int phase) =>
        (unit.Category(), phase) switch
        {
            (NexusUnitCategory.Strike, PhaseScreen) => true,
            (NexusUnitCategory.Strike, PhaseEngage) => true,
            (NexusUnitCategory.Capital, PhaseEngage) => true,
            (NexusUnitCategory.Planetary, PhaseBombard) => true,
            (NexusUnitCategory.Planetary, PhaseAssault) => true,
            _ => false,
        };

    private static NexusPhaseParticipation ToPhaseFlag(int phase) =>
        phase switch
        {
            PhaseScreen => NexusPhaseParticipation.Screen,
            PhaseEngage => NexusPhaseParticipation.Engage,
            PhaseBombard => NexusPhaseParticipation.Bombard,
            PhaseAssault => NexusPhaseParticipation.Assault,
            _ => NexusPhaseParticipation.None,
        };
}
