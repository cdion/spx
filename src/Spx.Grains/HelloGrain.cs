using Orleans;
using Spx.Contracts;

namespace Spx.Grains;

public sealed class HelloGrain : Grain, IHelloGrain
{
    public Task<string> SayHello(string name)
    {
        var normalizedName = string.IsNullOrWhiteSpace(name) ? "world" : name.Trim();
        return Task.FromResult($"Hello, {normalizedName}. The response came from Orleans grain '{this.GetPrimaryKeyString()}'.");
    }
}