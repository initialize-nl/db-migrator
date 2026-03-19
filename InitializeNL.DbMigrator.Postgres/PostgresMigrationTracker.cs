using System.Data;
using Dapper;

namespace InitializeNL.DbMigrator.Postgres;

/// <summary>
/// PostgreSQL-based migration tracker with configurable table names and audit logging.
/// </summary>
public class PostgresMigrationTracker : IMigrationTracker
{
  private readonly PostgresMigrationOptions _options;

  public PostgresMigrationTracker() : this(new PostgresMigrationOptions())
  {
  }

  public PostgresMigrationTracker(PostgresMigrationOptions options)
  {
    _options = options;
  }

  public async Task InitAsync(IDbConnection connection, CancellationToken ct = default)
  {
    string sql = $"""
                  CREATE TABLE IF NOT EXISTS {_options.QuotedHistoryTableName} (
                    id          TEXT        PRIMARY KEY,
                    duration_ms BIGINT,
                    executed_by TEXT,
                    created_on  TIMESTAMPTZ DEFAULT (NOW() AT TIME ZONE 'utc')
                  );

                  CREATE TABLE IF NOT EXISTS {_options.QuotedHistoryLogTableName} (
                    id           BIGINT      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    migration_id TEXT        NOT NULL,
                    action       TEXT        NOT NULL,
                    duration_ms  BIGINT,
                    executed_by  TEXT,
                    created_on   TIMESTAMPTZ DEFAULT (NOW() AT TIME ZONE 'utc')
                  );

                  CREATE TABLE IF NOT EXISTS {_options.QuotedLockTableName} (
                    one        INT         PRIMARY KEY CHECK (one = 1),
                    created_on TIMESTAMPTZ DEFAULT (NOW() AT TIME ZONE 'utc')
                  );
                  """;
    await connection.ExecuteAsync(sql);
  }

  public async Task<IReadOnlyList<string>> GetAppliedAsync(IDbConnection connection, CancellationToken ct = default)
  {
    string sql = $"""
                  SELECT DISTINCT id
                  FROM   {_options.QuotedHistoryTableName}
                  ORDER  BY id
                  """;
    IEnumerable<string> result = await connection.QueryAsync<string>(sql);
    return result.ToList();
  }

  public async Task AddAsync(
    IDbConnection connection,
    string migrationId,
    long? durationMs = null,
    CancellationToken ct = default)
  {
    string executedBy = Environment.MachineName;

    string insertSql = $"""
                        INSERT INTO {_options.QuotedHistoryTableName} (id, duration_ms, executed_by, created_on)
                        VALUES      (@Id, @DurationMs, @ExecutedBy, NOW())
                        """;
    await connection.ExecuteAsync(
      insertSql,
      new {Id = migrationId, DurationMs = durationMs, ExecutedBy = executedBy});

    string logSql = $"""
                     INSERT INTO {_options.QuotedHistoryLogTableName} (migration_id, action, duration_ms, executed_by)
                     Values      (@Id, 'up', @DurationMs, @ExecutedBy)
                     """;
    await connection.ExecuteAsync(
      logSql,
      new {Id = migrationId, DurationMs = durationMs, ExecutedBy = executedBy});
  }

  public async Task RemoveAsync(
    IDbConnection connection,
    string migrationId,
    long? durationMs = null,
    CancellationToken ct = default)
  {
    string executedBy = Environment.MachineName;

    string deleteSql = $"""
                        DELETE FROM {_options.QuotedHistoryTableName}
                        WHERE       id = @Id
                        """;
    await connection.ExecuteAsync(deleteSql, new {Id = migrationId});

    string logSql = $"""
                     INSERT INTO {_options.QuotedHistoryLogTableName} (migration_id, action, duration_ms, executed_by)
                     VALUES      (@Id, 'down', @DurationMs, @ExecutedBy)
                     """;
    await connection.ExecuteAsync(
      logSql,
      new {Id = migrationId, DurationMs = durationMs, ExecutedBy = executedBy});
  }
}