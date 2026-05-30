using Orleans.Hosting;

namespace Spx.Silo.Infrastructure;

internal static class SiloBuilderExtensions
{
    internal static ISiloBuilder AddAdoNetGrainStorageAsDefault(
        this ISiloBuilder siloBuilder,
        string connectionString
    )
    {
        siloBuilder.AddAdoNetGrainStorageAsDefault(optionsBuilder =>
        {
            optionsBuilder.Configure(options =>
            {
                options.Invariant = "Npgsql";
                options.ConnectionString = connectionString;
            });
        });
        return siloBuilder;
    }
}
