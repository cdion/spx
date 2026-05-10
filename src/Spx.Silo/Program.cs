using Spx.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddKeyedRedisClient("orleans-redis");
builder.UseOrleans();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", static () => Results.Ok(new
{
	message = "Orleans starter is running.",
	status = "Use the web app for authenticated Orleans calls."
}));

app.Run();
