namespace Spx.Nexus.Domain;

/// <summary>
/// Combat rules for Nexus Protocol.
/// <para>Each unit has a base hit threshold and tags that control targeting permissions
/// (<c>CanTargetStrike</c>, <c>CanTargetCapital</c>, <c>CanTargetPlanetary</c>).
/// <c>PreferredTargets</c> lowers the threshold by 1; <c>DifficultTargets</c> raises it by 1.
/// The result is clamped at a minimum of 2. If the unit lacks the appropriate
/// <c>CanTarget*</c> tag for the target's category, it cannot target that unit.</para>
/// </summary>
public static class NexusCombatSpec
{
    private const int MinimumHitThreshold = 2;

    /// <summary>
    /// Returns the minimum d6 roll needed for <paramref name="attacker"/> to score a hit on
    /// <paramref name="target"/>, or <c>null</c> if that attacker cannot target that unit.
    /// </summary>
    public static int? GetHitThreshold(NexusUnitType attacker, NexusUnitType target)
    {
        var profile = attacker.Profile();
        var targetCategory = target.Profile().Category;

        if (!profile.Tags.HasFlag(TagForCategory(targetCategory)))
            return null;

        var threshold = profile.HitThreshold;

        // Apply bonus (threshold -1) or penalty (threshold +1) for the target's category
        if (profile.Tags.HasFlag(BonusForCategory(targetCategory)))
            threshold--;
        else if (profile.Tags.HasFlag(PenaltyForCategory(targetCategory)))
            threshold++;

        return Math.Max(MinimumHitThreshold, threshold);
    }

    private static NexusUnitTag BonusForCategory(NexusUnitCategory category) =>
        category switch
        {
            NexusUnitCategory.Strike => NexusUnitTag.BonusVsStrike,
            NexusUnitCategory.Capital => NexusUnitTag.BonusVsCapital,
            NexusUnitCategory.Planetary => NexusUnitTag.BonusVsPlanetary,
            _ => NexusUnitTag.None,
        };

    private static NexusUnitTag PenaltyForCategory(NexusUnitCategory category) =>
        category switch
        {
            NexusUnitCategory.Strike => NexusUnitTag.PenaltyVsStrike,
            NexusUnitCategory.Capital => NexusUnitTag.PenaltyVsCapital,
            NexusUnitCategory.Planetary => NexusUnitTag.PenaltyVsPlanetary,
            _ => NexusUnitTag.None,
        };

    /// <summary>Returns the <c>CanTarget*</c> tag for the given category.</summary>
    public static NexusUnitTag TagForCategory(NexusUnitCategory category) =>
        category switch
        {
            NexusUnitCategory.Strike => NexusUnitTag.CanTargetStrike,
            NexusUnitCategory.Capital => NexusUnitTag.CanTargetCapital,
            NexusUnitCategory.Planetary => NexusUnitTag.CanTargetPlanetary,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
        };
}
