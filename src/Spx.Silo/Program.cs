using Orleans.Configuration;
using Orleans.Hosting;
using Spx.Silo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var orleansClusterId = builder.Configuration["Orleans:ClusterId"] ?? "spx-local-cluster";
var orleansServiceId = builder.Configuration["Orleans:ServiceId"] ?? "spx-local-service";

builder.AddServiceDefaults();
builder.AddKeyedNpgsqlDataSource("orleansdb");
builder.AddKeyedRedisClient("orleans-redis");
builder.UseOrleans(siloBuilder =>
{
    siloBuilder.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = orleansClusterId;
        options.ServiceId = orleansServiceId;
    });

    siloBuilder.UseRedisClustering(
        builder.Configuration.GetConnectionString("orleans-redis")
            ?? throw new InvalidOperationException(
                "Connection string 'orleans-redis' was not configured."
            )
    );

    siloBuilder.AddAdoNetGrainStorageAsDefault(
        builder.Configuration.GetConnectionString("orleansdb")
            ?? throw new InvalidOperationException(
                "Connection string 'orleansdb' was not configured."
            )
    );
});

var app = builder.Build();

await OrleansStorageSchemaBootstrapper.BootstrapAsync(
    app.Services,
    app.Logger,
    app.Lifetime.ApplicationStopping
);

app.MapDefaultEndpoints();

app.MapGet(
    "/",
    static () =>
        Results.Ok(
            new
            {
                message = "Orleans starter is running.",
                status = "Use the web app for authenticated Orleans calls.",
            }
        )
);

app.Run();
