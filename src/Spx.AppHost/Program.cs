var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("orleans-redis");

var orleans = builder.AddOrleans("cluster")
    .WithClustering(redis);

var silo = builder.AddProject<Projects.Spx_Silo>("silo")
    .WithReference(orleans)
    .WaitFor(redis)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Spx_Web>("web")
    .WithReference(orleans.AsClient())
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WaitFor(silo);

builder.Build().Run();