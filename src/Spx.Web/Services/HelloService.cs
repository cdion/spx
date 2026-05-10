using Orleans;
using Spx.Contracts;

namespace Spx.Web.Services;

public sealed class HelloService(IClusterClient clusterClient) : IHelloService
{
    public Task<string> SayHelloAsync(string userId, string name)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("The current request is missing an authenticated user id.");
        }

        var normalizedName = string.IsNullOrWhiteSpace(name) ? "world" : name.Trim();
        var grain = clusterClient.GetGrain<IHelloGrain>(normalizedName.ToLowerInvariant());
        return grain.SayHello(userId, normalizedName);
    }
}