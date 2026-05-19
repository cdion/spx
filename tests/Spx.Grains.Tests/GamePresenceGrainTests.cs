using Spx.Grains;
using Xunit;

namespace Spx.Grains.Tests;

public sealed class GamePresenceGrainTests
{
    [Fact]
    public void GamePresenceGrain_does_not_use_persistent_state()
    {
        Assert.Equal(typeof(Grain), typeof(GamePresenceGrain).BaseType);

        var constructorParameters = typeof(GamePresenceGrain)
            .GetConstructors()
            .SelectMany(constructor => constructor.GetParameters());

        Assert.DoesNotContain(
            constructorParameters,
            parameter =>
                parameter.ParameterType.IsGenericType
                && parameter.ParameterType.GetGenericTypeDefinition() == typeof(IPersistentState<>)
        );
    }
}
