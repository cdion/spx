using System.Collections.Immutable;
using Orleans;

namespace Spx.Game.Domain;

// Kept for lobby infrastructure compatibility (EnsureSessionAsync etc.)
[GenerateSerializer]
[Immutable]
public sealed record GameSessionParticipant([property: Id(0)] Guid PlayerId);

[GenerateSerializer]
[Immutable]
public sealed record InitializeNexusGameCommand(
    [property: Id(0)] GameSessionParticipant FirstPlayer,
    [property: Id(1)] GameSessionParticipant SecondPlayer
);

[GenerateSerializer]
[Immutable]
public abstract record NexusFleetOrder([property: Id(0)] Guid FleetId);

[GenerateSerializer]
[Immutable]
public sealed record NexusMoveOrder(Guid FleetId, [property: Id(1)] HexCoord Destination)
    : NexusFleetOrder(FleetId);

[GenerateSerializer]
[Immutable]
public sealed record NexusColonizeOrder(Guid FleetId) : NexusFleetOrder(FleetId);

[GenerateSerializer]
[Immutable]
public sealed record NexusTurnOrdersCommand(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] int ExpectedRoundNumber,
    [property: Id(2)] ImmutableArray<NexusFleetOrder> FleetOrders,
    [property: Id(3)] bool BuildFleet,
    [property: Id(4)] bool BeginNexusGate
);

[GenerateSerializer]
[Alias("NexusTurnOrdersResult")]
[Immutable]
public abstract record NexusTurnOrdersResult;

[GenerateSerializer]
[Immutable]
public sealed record NexusTurnOrdersAccepted : NexusTurnOrdersResult;

[GenerateSerializer]
[Immutable]
public sealed record NexusTurnOrdersRejected([property: Id(0)] string ErrorMessage)
    : NexusTurnOrdersResult;
