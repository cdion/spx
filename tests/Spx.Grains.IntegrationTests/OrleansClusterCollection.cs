using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.TestingHost;
using Spx.Grains;
using Xunit;

namespace Spx.Grains.IntegrationTests;

public sealed class OrleansClusterFixture : IAsyncLifetime
{
    public InProcessTestCluster Cluster { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = new InProcessTestClusterBuilder();

        builder.ConfigureSilo(
            (_, siloBuilder) =>
            {
                siloBuilder.AddMemoryGrainStorageAsDefault();

                siloBuilder.Configure<GrainCollectionOptions>(options =>
                {
                    options.CollectionQuantum = TimeSpan.FromSeconds(1);
                    options.ClassSpecificCollectionAge[typeof(GamePresenceGrain).FullName!] =
                        TimeSpan.FromSeconds(2);
                    options.ClassSpecificCollectionAge[typeof(NexusGameSessionGrain).FullName!] =
                        TimeSpan.FromSeconds(2);
                });
            }
        );

        Cluster = builder.Build();
        await Cluster.DeployAsync();
    }

    public async Task DisposeAsync()
    {
        if (Cluster is not null)
        {
            await Cluster.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class OrleansClusterCollection : ICollectionFixture<OrleansClusterFixture>
{
    public const string Name = nameof(OrleansClusterCollection);
}
