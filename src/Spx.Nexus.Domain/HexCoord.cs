using Orleans;

namespace Spx.Nexus.Domain;

[GenerateSerializer]
public readonly record struct HexCoord([property: Id(0)] int Q, [property: Id(1)] int R)
{
    private static readonly HexCoord[] Directions =
    [
        new(1, 0),
        new(1, -1),
        new(0, -1),
        new(-1, 0),
        new(-1, 1),
        new(0, 1),
    ];

    public HexCoord[] GetNeighbours()
    {
        var result = new HexCoord[6];
        for (var i = 0; i < 6; i++)
            result[i] = new HexCoord(Q + Directions[i].Q, R + Directions[i].R);
        return result;
    }

    public int DistanceTo(HexCoord other)
    {
        var dq = Q - other.Q;
        var dr = R - other.R;
        return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(dq + dr)) / 2;
    }

    public override string ToString() => $"({Q},{R})";
}
