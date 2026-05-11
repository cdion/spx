using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Spx.Data;

internal static class GamePersistenceErrors
{
    public static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}