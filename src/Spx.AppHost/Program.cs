var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("orleans-redis");
var postgresPassword = builder.AddParameter(
    "postgres-password",
    () => builder.Configuration["Parameters:postgres-password"] ?? "spx-local-postgres-password",
    secret: true);

var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithDataVolume("spx-postgres-data");
var appDb = postgres.AddDatabase("appdb");
var orleansDb = postgres.AddDatabase("orleansdb");

var orleans = builder.AddOrleans("cluster")
    .WithClustering(redis);

var silo = builder.AddProject<Projects.Spx_Silo>("silo")
    .WithReference(orleans)
    .WithReference(orleansDb)
    .WaitFor(redis)
    .WaitFor(orleansDb)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Spx_Web>("web")
    .WithReference(orleans.AsClient())
    .WithReference(appDb)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WaitFor(appDb)
    .WaitFor(silo);

builder.Build().Run();