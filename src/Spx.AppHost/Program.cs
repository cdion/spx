var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("orleans-redis");
var postgres = builder.AddPostgres("postgres");
var identityDb = postgres.AddDatabase("identitydb");

var orleans = builder.AddOrleans("cluster")
    .WithClustering(redis);

var silo = builder.AddProject<Projects.Spx_Silo>("silo")
    .WithReference(orleans)
    .WaitFor(redis)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Spx_Web>("web")
    .WithReference(orleans.AsClient())
    .WithReference(identityDb)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WaitFor(identityDb)
    .WaitFor(silo);

builder.Build().Run();