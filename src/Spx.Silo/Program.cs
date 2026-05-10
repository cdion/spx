using Spx.Silo.Infrastructure;
using Spx.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddKeyedNpgsqlDataSource("orleansdb");
builder.AddKeyedRedisClient("orleans-redis");
builder.UseOrleans();

var app = builder.Build();

await OrleansStorageSchemaBootstrapper.BootstrapAsync(
	app.Services,
	app.Logger,
	app.Lifetime.ApplicationStopping);

app.MapDefaultEndpoints();

app.MapGet("/", static () => Results.Ok(new
{
	message = "Orleans starter is running.",
	status = "Use the web app for authenticated Orleans calls."
}));

app.Run();
