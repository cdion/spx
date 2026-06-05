namespace Spx.Nexus.Domain;

/// <summary>
/// Combat rules for Nexus Protocol.
/// <para>Each unit has a base hit threshold and tags that control targeting permissions
/// (<c>CanAttack*</c> for Battle phase, <c>FirstAttack*</c> for Contact phase).
/// <c>BonusVs*</c> lowers the threshold by 1; <c>PenaltyVs*</c> raises it by 1.
/// The result is clamped at a minimum of 2. If the unit lacks the appropriate
/// targeting tag for the target's category, it cannot target that unit.</para>
/// </summary>
public static class NexusCombatSpec
{
    private const int MinimumHitThreshold = 2;

    /// <summary>
    /// Returns the minimum d6 roll needed for <paramref name="attacker"/> to score a hit on
    /// <paramref name="target"/>, or <c>null</c> if that attacker cannot target that unit
    /// in the given <paramref name="phase"/>.
    /// </summary>
    public static int? GetHitThreshold(
        NexusUnitType attacker,
        NexusUnitType target,
        NexusCombatPhase phase = NexusCombatPhase.Battle
    )
    {
        var profile = attacker.Profile();
        var targetCategory = target.Profile().Category;

        if (!CanTargetCategory(profile.Tags, targetCategory, phase))
            return null;

        var threshold = profile.HitThreshold;

        // Apply bonus (threshold -1) or penalty (threshold +1) for the target's category
        if (profile.Tags.HasFlag(BonusForCategory(targetCategory)))
            threshold--;
        else if (profile.Tags.HasFlag(PenaltyForCategory(targetCategory)))
            threshold++;

        return Math.Max(MinimumHitThreshold, threshold);
    }

    /// <summary>
    /// Returns true if the unit with the given <paramref name="tags"/> can target
    /// <paramref name="category"/> in the specified <paramref name="phase"/>.
    /// </summary>
    public static bool CanTargetCategory(
        NexusUnitTag tags,
        NexusUnitCategory category,
        NexusCombatPhase phase
    ) =>
        phase switch
        {
            NexusCombatPhase.Contact => tags.HasFlag(FirstAttackForCategory(category)),
            NexusCombatPhase.Battle => tags.HasFlag(CanAttackForCategory(category)),
            _ => false,
        };

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

    /// <summary>Returns the <c>CanAttack*</c> tag for the given category (Battle phase targeting).</summary>
    public static NexusUnitTag CanAttackForCategory(NexusUnitCategory category) =>
        category switch
        {
            NexusUnitCategory.Strike => NexusUnitTag.CanAttackStrike,
            NexusUnitCategory.Capital => NexusUnitTag.CanAttackCapital,
            NexusUnitCategory.Planetary => NexusUnitTag.CanAttackPlanetary,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
        };

    /// <summary>Returns the <c>FirstAttack*</c> tag for the given category (Contact phase targeting).</summary>
    public static NexusUnitTag FirstAttackForCategory(NexusUnitCategory category) =>
        category switch
        {
            NexusUnitCategory.Strike => NexusUnitTag.FirstAttackStrike,
            NexusUnitCategory.Capital => NexusUnitTag.FirstAttackCapital,
            NexusUnitCategory.Planetary => NexusUnitTag.FirstAttackPlanetary,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
        };

    /// <summary>
    /// Computes targeting silhouette weights for a list of individual units.
    /// Each Escort unit protects one non-Escort Capital ship by reducing its
    /// effective silhouette by 1 (minimum 1). The ships with the highest
    /// silhouette are covered first. The returned array has one entry per
    /// input unit (index-aligned).
    /// </summary>
    public static int[] ComputeTargetWeights(IReadOnlyList<NexusUnitType> units)
    {
        var weights = new int[units.Count];

        var escortCount = units.Count(t => t.Profile().Tags.HasFlag(NexusUnitTag.Escort));

        var protectable = units
            .Select((t, i) => (Type: t, Index: i))
            .Where(x => x.Type.IsCapital() && !x.Type.Profile().Tags.HasFlag(NexusUnitTag.Escort))
            .OrderByDescending(x => x.Type.Profile().Silhouette)
            .ToList();

        var protectedCount = Math.Min(escortCount, protectable.Count);
        var protectedIndices = protectable.Take(protectedCount).Select(x => x.Index).ToHashSet();

        for (var i = 0; i < units.Count; i++)
        {
            var sil = units[i].Profile().Silhouette;
            if (protectedIndices.Contains(i))
                sil = Math.Max(1, sil - 1);
            weights[i] = sil;
        }

        return weights;
    }
}
