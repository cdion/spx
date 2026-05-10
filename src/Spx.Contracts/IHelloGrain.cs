using Orleans;

namespace Spx.Contracts;

public interface IHelloGrain : IGrainWithStringKey
{
    Task<string> SayHello(string name);
}