using Spx.Grains;
using Xunit;

namespace Spx.Grains.Tests;

public sealed class HelloGrainTests
{
    [Fact]
    public void BuildGreeting_TrimsInputs_AndIncludesGrainKey()
    {
        var result = HelloGrain.BuildGreeting("  user-123  ", "  Orleans  ", "hello-grain");

        Assert.Equal("Hello, Orleans. Authenticated user 'user-123' reached Orleans grain 'hello-grain'.", result);
    }
}