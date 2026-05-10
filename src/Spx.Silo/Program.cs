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
	tryEndpoint = "/hello/copilot"
}));

app.MapGet("/hello/{name}",
	static async (IGrainFactory grains, string name) =>
	{
		var grain = grains.GetGrain<IHelloGrain>(name.ToLowerInvariant());
		var message = await grain.SayHello(name);
		return Results.Ok(new { message });
	});

app.Run();
