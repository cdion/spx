namespace Spx.Nexus.Domain;

/// <summary>The two combat phases: first strike fires first, then battle resolves remaining units.</summary>
public enum NexusCombatPhase
{
    FirstStrike = 0,
    Battle = 1,
}

/// <summary>Extensions for <see cref="NexusCombatPhase"/>.</summary>
public static class NexusCombatPhaseExtensions
{
    /// <summary>Returns the human-readable display name for this phase.</summary>
    public static string DisplayName(this NexusCombatPhase phase) =>
        phase switch
        {
            NexusCombatPhase.FirstStrike => "First Strike",
            NexusCombatPhase.Battle => "Battle",
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null),
        };
}

public enum NexusFactionColor
{
    Red = 0,
    Blue = 1,
}

public enum NexusGateProgress
{
    None = 0,
    Started = 1,
    Completed = 2,
}

/// <summary>Tags model special abilities that affect combat behavior.</summary>
[Flags]
public enum NexusUnitTag
{
    None = 0,

    /// <summary>Absorbs the first hit each combat on a 4+ save.</summary>
    Shield = 1,

    /// <summary>Fires before normal units; kills from first strike don't fire back.</summary>
    FirstStrike = 2,

    /// <summary>Can target Strike-category units.</summary>
    CanTargetStrike = 4,

    /// <summary>Can target Capital-category units.</summary>
    CanTargetCapital = 8,

    /// <summary>Can target Planetary-category units.</summary>
    CanTargetPlanetary = 16,

    /// <summary>Hit threshold -1 vs Strike targets.</summary>
    BonusVsStrike = 32,

    /// <summary>Hit threshold -1 vs Capital targets.</summary>
    BonusVsCapital = 64,

    /// <summary>Hit threshold -1 vs Planetary targets.</summary>
    BonusVsPlanetary = 128,

    /// <summary>Hit threshold +1 vs Strike targets.</summary>
    PenaltyVsStrike = 256,

    /// <summary>Hit threshold +1 vs Capital targets.</summary>
    PenaltyVsCapital = 512,

    /// <summary>Hit threshold +1 vs Planetary targets.</summary>
    PenaltyVsPlanetary = 1024,

    /// <summary>Attacks bypass Shield saves entirely.</summary>
    IgnoreShield = 2048,

    /// <summary>One extra attack each step that only targets Strike units.</summary>
    FreeAttackVsStrike = 4096,

    /// <summary>One extra attack each step that only targets Capital units.</summary>
    FreeAttackVsCapital = 8192,

    /// <summary>One extra attack each step that only targets Planetary units.</summary>
    FreeAttackVsPlanetary = 16384,

    /// <summary>Reduces effective silhouette of friendly non-Escort Capital units by 1 (min 1). Does not stack.</summary>
    Escort = 32768,
}
