using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Spx.Silo.Infrastructure;

internal static partial class OrleansStorageSchemaBootstrapper
{
    private const string OrleansDatabaseKey = "orleansdb";
    private static readonly string[] PersistenceQueryKeys =
    [
        "WriteToStorageKey",
        "ReadFromStorageKey",
        "ClearStorageKey",
        "DeleteStorageKey",
    ];

    public static async Task BootstrapAsync(
        IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        await using var scope = services.CreateAsyncScope();
        var dataSource = scope.ServiceProvider.GetRequiredKeyedService<NpgsqlDataSource>(
            OrleansDatabaseKey
        );
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var hasOrleansQueryTable = await TableExistsAsync(
            connection,
            "orleansquery",
            cancellationToken
        );

        if (!hasOrleansQueryTable)
        {
            LogBootstrappingMainSchema(logger);
            await ExecuteScriptAsync(connection, "Orleans.PostgreSQL-Main.sql", cancellationToken);
            hasOrleansQueryTable = true;
        }

        var persistenceQueryCount = await GetPersistenceQueryCountAsync(
            connection,
            cancellationToken
        );
        var hasOrleansStorageTable = await TableExistsAsync(
            connection,
            "orleansstorage",
            cancellationToken
        );

        if (!hasOrleansStorageTable)
        {
            if (persistenceQueryCount > 0)
            {
                throw new InvalidOperationException(
                    "Detected a partially configured Orleans persistence schema. "
                        + "The OrleansStorage table is missing but persistence query registrations already exist."
                );
            }

            LogBootstrappingPersistenceSchema(logger);
            await ExecuteScriptAsync(
                connection,
                "Orleans.PostgreSQL-Persistence.sql",
                cancellationToken
            );
            hasOrleansStorageTable = true;
            persistenceQueryCount = await GetPersistenceQueryCountAsync(
                connection,
                cancellationToken
            );
        }

        if (
            !hasOrleansQueryTable
            || !hasOrleansStorageTable
            || persistenceQueryCount != PersistenceQueryKeys.Length
        )
        {
            throw new InvalidOperationException(
                $"Orleans PostgreSQL persistence bootstrap is incomplete. "
                    + $"Query table: {hasOrleansQueryTable}, storage table: {hasOrleansStorageTable}, "
                    + $"registered persistence queries: {persistenceQueryCount}."
            );
        }

        LogPersistenceSchemaReady(logger);
    }

    private static async Task<bool> TableExistsAsync(
        NpgsqlConnection connection,
        string tableName,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT to_regclass('public.{tableName}') IS NOT NULL;";

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is true;
    }

    private static async Task<int> GetPersistenceQueryCountAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken
    )
    {
        if (!await TableExistsAsync(connection, "orleansquery", cancellationToken))
        {
            return 0;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM OrleansQuery
            WHERE QueryKey IN (
                'WriteToStorageKey',
                'ReadFromStorageKey',
                'ClearStorageKey',
                'DeleteStorageKey'
            );
            """;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, provider: null);
    }

    private static async Task ExecuteScriptAsync(
        NpgsqlConnection connection,
        string fileName,
        CancellationToken cancellationToken
    )
    {
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "Sql", fileName);

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException(
                "Orleans PostgreSQL bootstrap script was not found.",
                scriptPath
            );
        }

        var script = await File.ReadAllTextAsync(scriptPath, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = script;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Bootstrapping Orleans PostgreSQL main schema."
    )]
    private static partial void LogBootstrappingMainSchema(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Bootstrapping Orleans PostgreSQL persistence schema."
    )]
    private static partial void LogBootstrappingPersistenceSchema(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Orleans PostgreSQL persistence schema is ready."
    )]
    private static partial void LogPersistenceSchemaReady(ILogger logger);
}
