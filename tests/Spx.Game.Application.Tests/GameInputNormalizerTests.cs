using Spx.Game.Application;
using Xunit;

namespace Spx.Game.Application.Tests;

public sealed class GameInputNormalizerTests
{
    [Fact]
    public void NormalizeDisplayText_collapses_whitespace_and_trims_edges()
    {
        var normalized = GameInputNormalizer.NormalizeDisplayText("  Captain   Red  ");

        Assert.Equal("Captain Red", normalized);
    }

    [Fact]
    public void NormalizeLookupKey_uppercases_normalized_display_text()
    {
        var normalized = GameInputNormalizer.NormalizeLookupKey("  Captain   Red  ");

        Assert.Equal("CAPTAIN RED", normalized);
    }
}