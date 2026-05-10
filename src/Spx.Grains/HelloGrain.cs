using Orleans;
using Spx.Contracts;

namespace Spx.Grains;

public sealed class HelloGrain : Grain, IHelloGrain
{
    public Task<string> SayHello(string userId, string name)
        => Task.FromResult(BuildGreeting(userId, name, this.GetPrimaryKeyString()));

    internal static string BuildGreeting(string userId, string name, string grainKey)
    {
        var normalizedUserId = string.IsNullOrWhiteSpace(userId) ? "anonymous" : userId.Trim();
        var normalizedName = string.IsNullOrWhiteSpace(name) ? "world" : name.Trim();
        return $"Hello, {normalizedName}. Authenticated user '{normalizedUserId}' reached Orleans grain '{grainKey}'.";
    }
}