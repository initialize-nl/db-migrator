using System.Data;
using Dapper;
using Npgsql;

namespace InitializeNL.DbMigrator.Postgres;

/// <summary>
/// PostgreSQL table-based migration lock using a lock table.
/// Alternative to PostgresAdvisoryLock for environments that need a visible lock record.
/// </summary>
public class PostgresTableLock : IMigrationLock
{
  private readonly PostgresMigrationOptions _options;

  public PostgresTableLock() : this(new PostgresMigrationOptions())
  {
  }

  public PostgresTableLock(PostgresMigrationOptions options)
  {
    _options = options;
  }

  public async Task<bool> AcquireAsync(IDbConnection connection, CancellationToken ct = default)
  {
    if (connection is not NpgsqlConnection npgsqlConnection)
    {
      throw new InvalidOperationException("PostgresTableLock requires an NpgsqlConnection");
    }

    try
    {
      if (npgsqlConnection.State != ConnectionState.Open)
      {
        await npgsqlConnection.OpenAsync(ct);
      }

      await using NpgsqlTransaction trx = await npgsqlConnection.BeginTransactionAsync(
        IsolationLevel.RepeatableRead,
        ct);

      string sql = $"""
                    INSERT INTO {_options.QuotedLockTableName} (one, created_on)
                    VALUES      (1, NOW())
                    ON CONFLICT DO NOTHING
                    """;
      int rowsAffected = await npgsqlConnection.ExecuteAsync(sql);
      await trx.CommitAsync(ct);

      return rowsAffected > 0;
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
      string sql = $"""
                    DELETE FROM {_options.QuotedLockTableName}
                    WHERE       one = 1
                    """;
      await connection.ExecuteAsync(sql);
      return true;
    }
    catch
    {
      return false;
    }
  }
}