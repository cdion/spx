var builder = DistributedApplication.CreateBuilder(args);

const int SiloHttpPort = 5025;
const int SiloHttpsPort = 7000;
const int WebHttpPort = 5224;
const int WebHttpsPort = 7193;
const int RedisPort = 16379;
const int PostgresPort = 15432;

var redis = builder
    .AddRedis("orleans-redis")
    .WithEndpoint("tcp", endpoint => endpoint.Port = RedisPort)
    .WithLifetime(Aspire.Hosting.ApplicationModel.ContainerLifetime.Persistent);
var postgresPassword = builder.AddParameter(
    "postgres-password",
    () => builder.Configuration["Parameters:postgres-password"] ?? "spx-local-postgres-password",
    secret: true
);

var postgres = builder
    .AddPostgres("postgres", password: postgresPassword)
    .WithEndpoint("tcp", endpoint => endpoint.Port = PostgresPort)
    .WithLifetime(Aspire.Hosting.ApplicationModel.ContainerLifetime.Persistent)
    .WithDataVolume("spx-postgres-data");
var appDb = postgres.AddDatabase("appdb");
var orleansDb = postgres.AddDatabase("orleansdb");

var orleans = builder
    .AddOrleans("cluster")
    .WithClustering(redis)
    .WithServiceId("spx-local-service")
    .WithClusterId("spx-local-cluster");

var silo = builder
    .AddProject<Projects.Spx_Silo>("silo")
    .WithReference(orleans)
    .WithReference(orleansDb)
    .WithEndpoint("http", endpoint => endpoint.Port = SiloHttpPort)
    .WithEndpoint("https", endpoint => endpoint.Port = SiloHttpsPort)
    .WaitFor(redis)
    .WaitFor(orleansDb)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

builder
    .AddProject<Projects.Spx_Web>("web")
    .WithReference(orleans.AsClient())
    .WithReference(appDb)
    .WithEndpoint("http", endpoint => endpoint.Port = WebHttpPort)
    .WithEndpoint("https", endpoint => endpoint.Port = WebHttpsPort)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WaitFor(appDb)
    .WaitFor(silo);

builder.Build().Run();
