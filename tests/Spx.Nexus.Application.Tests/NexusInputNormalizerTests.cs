using Spx.Nexus.Application;
using Xunit;

namespace Spx.Nexus.Application.Tests;

public sealed class GameInputNormalizerTests
{
    [Fact]
    public void NormalizeDisplayText_collapses_whitespace_and_trims_edges()
    {
        var normalized = NexusInputNormalizer.NormalizeDisplayText("  Captain   Red  ");

        Assert.Equal("Captain Red", normalized);
    }

    [Fact]
    public void NormalizeLookupKey_uppercases_normalized_display_text()
    {
        var normalized = NexusInputNormalizer.NormalizeLookupKey("  Captain   Red  ");

        Assert.Equal("CAPTAIN RED", normalized);
    }
}
