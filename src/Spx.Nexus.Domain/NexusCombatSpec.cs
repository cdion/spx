namespace Spx.Nexus.Domain;

/// <summary>
/// Combat rules for Nexus Protocol.
/// <para>Each unit has a base hit threshold and tags that control targeting permissions
/// (<c>Battery</c> for Battle phase, <c>Vanguard</c> for Contact phase).
/// <c>Seeker</c> lowers the threshold by its magnitude; <c>Scatter</c> raises it.
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
        NexusUnitProfile attacker,
        NexusUnitProfile target,
        NexusCombatPhase phase = NexusCombatPhase.Battle,
        int commandBonus = 0
    )
    {
        if (!attacker.Attacks.TryGetValue(target.Category, out var spec))
            return null;
        var count = phase == NexusCombatPhase.Contact ? spec.Contact : spec.Battle;
        if (count == 0)
            return null;
        return Math.Max(MinimumHitThreshold, spec.Threshold - commandBonus);
    }

    /// <summary>
    /// Returns 1 if <paramref name="attacker"/> falls within the coverage of friendly
    /// <see cref="Command"/> modules targeting its category, 0 otherwise.
    /// Coverage = sum of N from all Command(Category) modules on other friendly units.
    /// The highest-silhouette eligible units (non-Command-providers) are covered first.
    /// </summary>
    public static int GetCommandBonus(
        NexusUnitProfile attacker,
        IReadOnlyList<NexusUnitProfile> friendlyProfiles
    )
    {
        var category = attacker.Category;

        // Units that provide Command for attacker's category (other than attacker itself)
        var coverage = friendlyProfiles
            .Where(p => !ReferenceEquals(p, attacker))
            .SelectMany(p => p.Modules.OfType<Command>().Where(c => c.Category == category))
            .Sum(c => c.N);

        if (coverage <= 0)
            return 0;

        // Eligible recipients: same category, not themselves a Command provider for this category
        var eligible = friendlyProfiles
            .Where(p => p.Category == category)
            .Where(p => !p.Modules.OfType<Command>().Any(c => c.Category == category))
            .OrderByDescending(p => p.Silhouette)
            .ToList();

        var rank = eligible.FindIndex(p => ReferenceEquals(p, attacker));
        return rank >= 0 && rank < coverage ? 1 : 0;
    }

    /// <summary>
    /// Computes targeting silhouette weights for a list of unit profiles.
    /// Each Screen(C) unit reduces the silhouette of one non-Screen Capital ship by 1 (min 1)
    /// when the attacker is of category C. The ships with the highest silhouette are covered first.
    /// The returned array has one entry per input profile (index-aligned).
    /// </summary>
    public static int[] ComputeTargetWeights(
        IReadOnlyList<NexusUnitProfile> profiles,
        NexusUnitCategory attackerCategory
    )
    {
        var weights = new int[profiles.Count];

        var escortCount = profiles.Sum(p =>
            p.Modules.OfType<Screen>().Where(s => s.Category == attackerCategory).Sum(s => s.N)
        );

        var protectable = profiles
            .Select((p, i) => (Profile: p, Index: i))
            .Where(x =>
                x.Profile.Category == NexusUnitCategory.Capital
                && !x.Profile.Modules.OfType<Screen>().Any()
            )
            .OrderByDescending(x => x.Profile.Silhouette)
            .ToList();

        var protectedCount = Math.Min(escortCount, protectable.Count);
        var protectedIndices = protectable.Take(protectedCount).Select(x => x.Index).ToHashSet();

        for (var i = 0; i < profiles.Count; i++)
        {
            var sil = profiles[i].Silhouette;
            if (protectedIndices.Contains(i))
                sil = Math.Max(1, sil - 1);
            weights[i] = sil;
        }

        return weights;
    }
}
