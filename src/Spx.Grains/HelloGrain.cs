using Orleans;
using Spx.Contracts;

namespace Spx.Grains;

public sealed class HelloGrain : Grain, IHelloGrain
{
    public Task<string> SayHello(string userId, string name)
    {
        var normalizedUserId = string.IsNullOrWhiteSpace(userId) ? "anonymous" : userId.Trim();
        var normalizedName = string.IsNullOrWhiteSpace(name) ? "world" : name.Trim();
        return Task.FromResult($"Hello, {normalizedName}. Authenticated user '{normalizedUserId}' reached Orleans grain '{this.GetPrimaryKeyString()}'.");
    }
}