using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;

namespace Spx.Silo.Infrastructure;

internal static class SiloBuilderExtensions
{
    internal static ISiloBuilder AddFaultTolerantAdoNetGrainStorageAsDefault(
        this ISiloBuilder siloBuilder,
        string connectionString
    )
    {
        siloBuilder.Services.AddSingleton<FaultTolerantJsonGrainStorageSerializer>();
        siloBuilder.AddAdoNetGrainStorageAsDefault(optionsBuilder =>
        {
            optionsBuilder.Configure(options =>
            {
                options.Invariant = "Npgsql";
                options.ConnectionString = connectionString;
            });
            optionsBuilder.Configure<FaultTolerantJsonGrainStorageSerializer>(
                (options, serializer) => options.GrainStorageSerializer = serializer
            );
        });
        return siloBuilder;
    }
}
