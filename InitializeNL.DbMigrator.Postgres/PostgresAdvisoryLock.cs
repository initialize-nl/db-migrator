using System.Data;
using Dapper;
using Npgsql;

namespace InitializeNL.DbMigrator.Postgres;

/// <summary>
/// PostgreSQL advisory lock implementation.
/// Uses session-level advisory locks which are automatically released on disconnect.
/// Lock scope is per-database (not server-wide).
/// </summary>
public class PostgresAdvisoryLock : IMigrationLock
{
  private readonly long _lockKey;

  /// <summary>
  /// Creates a new advisory lock with the specified key.
  /// </summary>
  /// <param name="lockKey">The advisory lock key. Default is a fixed key for database migrations.</param>
  public PostgresAdvisoryLock(long lockKey = 8_675_309)
  {
    _lockKey = lockKey;
  }

  public async Task<bool> AcquireAsync(IDbConnection connection, CancellationToken ct = default)
  {
    if (connection is not NpgsqlConnection npgsqlConnection)
    {
      throw new InvalidOperationException("PostgresAdvisoryLock requires an NpgsqlConnection");
    }

    try
    {
      if (npgsqlConnection.State != ConnectionState.Open)
      {
        await npgsqlConnection.OpenAsync(ct);
      }

      bool acquired = await npgsqlConnection.QuerySingleAsync<bool>(
        "SELECT pg_try_advisory_lock(@LockKey)",
        new {LockKey = _lockKey});

      return acquired;
    }
    catch
    {
      return false;
    }
  }

  public async Task<bool> ReleaseAsync(IDbConnection connection, CancellationToken ct = default)
  {
    try
    {
      bool released = await connection.QuerySingleAsync<bool>(
        "SELECT pg_advisory_unlock(@LockKey)",
        new {LockKey = _lockKey});

      return released;
    }
    catch
    {
      return false;
    }
  }
}