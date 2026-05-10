var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("orleans-redis");
var postgres = builder.AddPostgres("postgres");
var identityDb = postgres.AddDatabase("identitydb");
var orleansDb = postgres.AddDatabase("orleansdb");

var orleans = builder.AddOrleans("cluster")
    .WithClustering(redis)
    .WithGrainStorage("Default", orleansDb);

var silo = builder.AddProject<Projects.Spx_Silo>("silo")
    .WithReference(orleans)
    .WaitFor(redis)
    .WaitFor(orleansDb)
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