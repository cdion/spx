using Orleans;
using Spx.Contracts;

namespace Spx.Web.Services;

public sealed class HelloService(IClusterClient clusterClient) : IHelloService
{
    public Task<string> SayHelloAsync(string name)
    {
        var normalizedName = string.IsNullOrWhiteSpace(name) ? "world" : name.Trim();
        var grain = clusterClient.GetGrain<IHelloGrain>(normalizedName.ToLowerInvariant());
        return grain.SayHello(normalizedName);
    }
}