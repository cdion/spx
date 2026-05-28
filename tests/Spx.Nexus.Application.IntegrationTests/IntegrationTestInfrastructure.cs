using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Spx.Data;
using Xunit;

namespace Spx.Nexus.Application.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class IntegrationTestCollection : ICollectionFixture<PostgresDatabaseFixture>
{
    public const string Name = "Spx.Nexus.Application integration database";
}

public abstract class IntegrationTestBase(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    protected TestDatabase Database { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();
        Database = fixture.CreateDatabase();
    }

    public async Task DisposeAsync()
    {
        if (Database is not null)
        {
            await Database.DisposeAsync();
        }
    }
}

public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private const string PostgresPassword = "test-password";
    private const string PostgresDb = "testdb";
    private const string PostgresUser = "postgres";

    private IContainer? container;
    private Respawner? respawner;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        container = new ContainerBuilder("postgres:17-alpine")
            .WithEnvironment("POSTGRES_PASSWORD", PostgresPassword)
            .WithEnvironment("POSTGRES_DB", PostgresDb)
            .WithEnvironment("POSTGRES_USER", PostgresUser)
            .WithPortBinding(5432, true)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilMessageIsLogged("database system is ready to accept connections")
            )
            .Build();

        await container.StartAsync();

        var host = container.Hostname;
        var port = container.GetMappedPublicPort(5432);
        ConnectionString =
            $"Host={host};Port={port};Database={PostgresDb};Username={PostgresUser};Password={PostgresPassword}";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();

        await using (var connection = new NpgsqlConnection(ConnectionString))
        {
            await connection.OpenAsync();
            respawner = await Respawner.CreateAsync(
                connection,
                new RespawnerOptions
                {
                    DbAdapter = DbAdapter.Postgres,
                    SchemasToInclude = ["public"],
                    TablesToIgnore = [new Table("__EFMigrationsHistory")],
                }
            );
        }
    }

    public async Task DisposeAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }

    internal TestDatabase CreateDatabase()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException("The PostgreSQL fixture has not been initialized.");
        }

        return new TestDatabase(ConnectionString);
    }

    internal async Task ResetAsync()
    {
        if (respawner is null)
        {
            throw new InvalidOperationException("The PostgreSQL fixture has not been initialized.");
        }

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await respawner.ResetAsync(connection);
    }
}
