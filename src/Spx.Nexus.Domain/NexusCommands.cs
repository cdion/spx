using System.Collections.Immutable;
using Orleans;

namespace Spx.Nexus.Domain;

// Kept for lobby infrastructure compatibility (EnsureSessionAsync etc.)
[GenerateSerializer]
[Immutable]
public sealed record NexusSessionPlayer([property: Id(0)] Guid PlayerId);

[GenerateSerializer]
[Immutable]
public sealed record InitializeNexusGameCommand(
    [property: Id(0)] ImmutableArray<NexusSessionPlayer> Players
);

/// <summary>Build order for one design with a count.</summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusBuildOrder([property: Id(0)] Guid DesignId, [property: Id(1)] int Count);

/// <summary>
/// A single fleet move: pick a subset of units from <see cref="From"/> and move them to
/// the adjacent system <see cref="To"/>. Carrier capacity rules are enforced by the engine.
/// </summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusMoveOrder(
    [property: Id(0)] HexCoord From,
    [property: Id(1)] HexCoord To,
    [property: Id(2)] ImmutableArray<NexusUnitStackGroup> Stacks
);

/// <summary>
/// All orders a player submits for one round. Stored in grain state until both players
/// have submitted, then resolved together.
/// </summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusTurnOrdersCommand(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] int ExpectedRoundNumber,
    [property: Id(2)] ImmutableArray<NexusMoveOrder> MoveOrders,
    [property: Id(3)] ImmutableArray<NexusBuildOrder> BuildOrders,
    [property: Id(4)] bool BeginNexusGate
);

[GenerateSerializer]
[Alias("NexusTurnOrdersResult")]
[Immutable]
public abstract record NexusTurnOrdersResult;

[GenerateSerializer]
[Immutable]
public sealed record NexusTurnOrdersAccepted : NexusTurnOrdersResult;

// ── NexusTurnOrdersRejected discriminated union ──────────────────────────────

[GenerateSerializer]
[Alias("NexusTurnOrdersRejected")]
[Immutable]
public abstract record NexusTurnOrdersRejected(string ErrorMessage) : NexusTurnOrdersResult
{
    public static NexusTurnOrdersRejected GameOver() => new NexusRejectedGameOver();

    public static NexusTurnOrdersRejected RoundMismatch(int submitted, int current) =>
        new NexusRejectedRoundMismatch(submitted, current);

    public static NexusTurnOrdersRejected PlayerNotFound() => new NexusRejectedPlayerNotFound();

    public static NexusTurnOrdersRejected PlayerInactive() => new NexusRejectedPlayerInactive();

    public static NexusTurnOrdersRejected AlreadySubmitted() => new NexusRejectedAlreadySubmitted();

    public static NexusTurnOrdersRejected InvalidCoord(HexCoord coord, string label) =>
        new NexusRejectedInvalidCoord(coord, label);

    public static NexusTurnOrdersRejected NonAdjacent(
        HexCoord from,
        string fromLabel,
        HexCoord to,
        string toLabel
    ) => new NexusRejectedNonAdjacent(from, fromLabel, to, toLabel);

    public static NexusTurnOrdersRejected EmptyMove() => new NexusRejectedEmptyMove();

    public static NexusTurnOrdersRejected SystemNotFound(HexCoord coord, string label) =>
        new NexusRejectedSystemNotFound(coord, label);

    public static NexusTurnOrdersRejected PlanetaryMoveFromContested(
        HexCoord coord,
        string label
    ) => new NexusRejectedPlanetaryMoveFromContested(coord, label);

    public static NexusTurnOrdersRejected InvalidUnitCount(Guid designId) =>
        new NexusRejectedInvalidUnitCount(designId);

    public static NexusTurnOrdersRejected UnknownDesign(Guid designId, string context) =>
        new NexusRejectedUnknownDesign(designId, context);

    public static NexusTurnOrdersRejected InvalidRemainingHits(
        string designName,
        int value,
        int max
    ) => new NexusRejectedInvalidRemainingHits(designName, value, max);

    public static NexusTurnOrdersRejected InsufficientUnits(
        string designDesc,
        int requested,
        int available,
        HexCoord from,
        string fromLabel
    ) => new NexusRejectedInsufficientUnits(designDesc, requested, available, from, fromLabel);

    public static NexusTurnOrdersRejected InsufficientFleetCapacity(
        int requested,
        int available,
        HexCoord from,
        string fromLabel
    ) => new NexusRejectedInsufficientFleetCapacity(requested, available, from, fromLabel);

    public static NexusTurnOrdersRejected InsufficientFleetCapacityForMove(
        int requestedCarry,
        int providedCarry,
        HexCoord from,
        string fromLabel,
        HexCoord to,
        string toLabel
    ) =>
        new NexusRejectedInsufficientFleetCapacityForMove(
            requestedCarry,
            providedCarry,
            from,
            fromLabel,
            to,
            toLabel
        );

    public static NexusTurnOrdersRejected DesignNotOwned(string designName) =>
        new NexusRejectedDesignNotOwned(designName);

    public static NexusTurnOrdersRejected InsufficientEnergy(int required, int available) =>
        new NexusRejectedInsufficientEnergy(required, available);

    public static NexusTurnOrdersRejected GateAlreadyCompleted() =>
        new NexusRejectedGateAlreadyCompleted();

    public static NexusTurnOrdersRejected NoPlanetaryOnNexus() =>
        new NexusRejectedNoPlanetaryOnNexus();

    public static NexusTurnOrdersRejected NexusContested() => new NexusRejectedNexusContested();
}

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedGameOver : NexusTurnOrdersRejected
{
    public NexusRejectedGameOver()
        : base("Game is already over.") { }
}

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedRoundMismatch(
    [property: Id(0)] int Submitted,
    [property: Id(1)] int Current
) : NexusTurnOrdersRejected($"Round mismatch: submitted {Submitted}, current is {Current}");

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedPlayerNotFound : NexusTurnOrdersRejected
{
    public NexusRejectedPlayerNotFound()
        : base("Player not found.") { }
}

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedPlayerInactive : NexusTurnOrdersRejected
{
    public NexusRejectedPlayerInactive()
        : base("Player is not active.") { }
}

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedAlreadySubmitted : NexusTurnOrdersRejected
{
    public NexusRejectedAlreadySubmitted()
        : base("Orders already submitted this round.") { }
}

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedInvalidCoord(
    [property: Id(0)] HexCoord Coord,
    [property: Id(1)] string Label
) : NexusTurnOrdersRejected($"Selected {Label} System is not on the map.");

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedNonAdjacent(
    [property: Id(0)] HexCoord From,
    [property: Id(1)] string FromLabel,
    [property: Id(2)] HexCoord To,
    [property: Id(3)] string ToLabel
) : NexusTurnOrdersRejected($"{ToLabel} is not adjacent to {FromLabel}.");

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedEmptyMove : NexusTurnOrdersRejected
{
    public NexusRejectedEmptyMove()
        : base("A move order must include at least one unit.") { }
}

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedSystemNotFound(
    [property: Id(0)] HexCoord Coord,
    [property: Id(1)] string Label
) : NexusTurnOrdersRejected($"Source System {Label} was not found in the current game state.");

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedPlanetaryMoveFromContested(
    [property: Id(0)] HexCoord Coord,
    [property: Id(1)] string Label
) : NexusTurnOrdersRejected($"Planetary units cannot move from a contested system ({Label}).");

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedInvalidUnitCount([property: Id(0)] Guid DesignId)
    : NexusTurnOrdersRejected($"Unit count for design {DesignId} must be positive.");

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedUnknownDesign(
    [property: Id(0)] Guid DesignId,
    [property: Id(1)] string Context
) : NexusTurnOrdersRejected($"Unknown design {DesignId} in {Context} order.");

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedInvalidRemainingHits(
    [property: Id(0)] string DesignName,
    [property: Id(1)] int Value,
    [property: Id(2)] int Max
) : NexusTurnOrdersRejected($"Remaining hits for {DesignName} must be between 1 and {Max}.");

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedInsufficientUnits(
    [property: Id(0)] string DesignDesc,
    [property: Id(1)] int Requested,
    [property: Id(2)] int Available,
    [property: Id(3)] HexCoord From,
    [property: Id(4)] string FromLabel
)
    : NexusTurnOrdersRejected(
        $"Insufficient {DesignDesc} at {FromLabel}: need {Requested}, have {Available}."
    );

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedInsufficientFleetCapacity(
    [property: Id(0)] int Requested,
    [property: Id(1)] int Available,
    [property: Id(2)] HexCoord From,
    [property: Id(3)] string FromLabel
)
    : NexusTurnOrdersRejected(
        $"Insufficient Fleet Capacity at {FromLabel}: need {Requested}, have {Available}."
    );

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedInsufficientFleetCapacityForMove(
    [property: Id(0)] int RequestedCarry,
    [property: Id(1)] int ProvidedCarry,
    [property: Id(2)] HexCoord From,
    [property: Id(3)] string FromLabel,
    [property: Id(4)] HexCoord To,
    [property: Id(5)] string ToLabel
)
    : NexusTurnOrdersRejected(
        $"Insufficient Fleet Capacity for move from {FromLabel} to {ToLabel}: need {RequestedCarry}, have {ProvidedCarry}."
    );

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedDesignNotOwned([property: Id(0)] string DesignName)
    : NexusTurnOrdersRejected($"Design '{DesignName}' does not belong to this player.");

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedInsufficientEnergy(
    [property: Id(0)] int Required,
    [property: Id(1)] int Available
) : NexusTurnOrdersRejected($"Insufficient Energy: need {Required}, have {Available}.");

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedGateAlreadyCompleted : NexusTurnOrdersRejected
{
    public NexusRejectedGateAlreadyCompleted()
        : base("Nexus Gate is already completed.") { }
}

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedNoPlanetaryOnNexus : NexusTurnOrdersRejected
{
    public NexusRejectedNoPlanetaryOnNexus()
        : base("Cannot begin Nexus Gate: no planetary units on the Nexus.") { }
}

[GenerateSerializer]
[Immutable]
public sealed record NexusRejectedNexusContested : NexusTurnOrdersRejected
{
    public NexusRejectedNexusContested()
        : base("Cannot begin Nexus Gate: the Nexus is contested.") { }
}

// ── Out-of-band design management ────────────────────────────────────────────

[GenerateSerializer]
[Immutable]
public sealed record NexusCreateDesignCommand(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] string Name,
    [property: Id(2)] NexusUnitCategory Hull,
    [property: Id(3)] ImmutableArray<NexusUnitModule> Modules
);

[GenerateSerializer]
[Immutable]
public sealed record NexusDeleteDesignCommand(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] Guid DesignId
);

[GenerateSerializer]
[Alias("NexusDesignCommandResult")]
[Immutable]
public abstract record NexusDesignCommandResult;

[GenerateSerializer]
[Immutable]
public sealed record NexusDesignCreated([property: Id(0)] NexusUnitDesign Design)
    : NexusDesignCommandResult;

[GenerateSerializer]
[Immutable]
public sealed record NexusDesignDeleted : NexusDesignCommandResult;

// ── NexusDesignCommandRejected discriminated union ───────────────────────────

[GenerateSerializer]
[Alias("NexusDesignCommandRejected")]
[Immutable]
public abstract record NexusDesignCommandRejected(string ErrorMessage) : NexusDesignCommandResult
{
    public static NexusDesignCommandRejected PlayerNotFound() =>
        new NexusDesignRejectedPlayerNotFound();

    public static NexusDesignCommandRejected Validation(string error) =>
        new NexusDesignRejectedValidation(error);

    public static NexusDesignCommandRejected InUse(Guid designId) =>
        new NexusDesignRejectedInUse(designId);

    public static NexusDesignCommandRejected NotFound(Guid designId) =>
        new NexusDesignRejectedNotFound(designId);
}

[GenerateSerializer]
[Immutable]
public sealed record NexusDesignRejectedPlayerNotFound : NexusDesignCommandRejected
{
    public NexusDesignRejectedPlayerNotFound()
        : base("Player not found.") { }
}

[GenerateSerializer]
[Immutable]
public sealed record NexusDesignRejectedValidation([property: Id(0)] string Error)
    : NexusDesignCommandRejected(Error);

[GenerateSerializer]
[Immutable]
public sealed record NexusDesignRejectedInUse([property: Id(0)] Guid DesignId)
    : NexusDesignCommandRejected(
        "Cannot delete a design while units of that design are on the map."
    );

[GenerateSerializer]
[Immutable]
public sealed record NexusDesignRejectedNotFound([property: Id(0)] Guid DesignId)
    : NexusDesignCommandRejected("Design not found.");
