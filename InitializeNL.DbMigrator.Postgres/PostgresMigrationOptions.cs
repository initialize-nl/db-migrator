using PgQuoteIdent;

namespace InitializeNL.DbMigrator.Postgres;

/// <summary>
/// Configuration options for PostgreSQL migration table names.
/// Names are quoted using PostgreSQL's identifier quoting rules to prevent SQL injection.
/// </summary>
public record PostgresMigrationOptions
{
  /// <summary>
  /// Name of the table that tracks applied migrations.
  /// </summary>
  public string HistoryTableName { get; init; } = "schema_migration_history";

  /// <summary>
  /// Name of the append-only audit log table.
  /// </summary>
  public string HistoryLogTableName { get; init; } = "schema_migration_history_log";

  /// <summary>
  /// Name of the table used for distributed locking (only used by PostgresTableLock).
  /// </summary>
  public string LockTableName { get; init; } = "schema_migration_lock";

  /// <summary>
  /// Returns the history table name as a safely quoted PostgreSQL identifier.
  /// </summary>
  internal string QuotedHistoryTableName => PgQuoteIdentifier.QuoteIdentifier(HistoryTableName);

  /// <summary>
  /// Returns the history log table name as a safely quoted PostgreSQL identifier.
  /// </summary>
  internal string QuotedHistoryLogTableName => PgQuoteIdentifier.QuoteIdentifier(HistoryLogTableName);

  /// <summary>
  /// Returns the lock table name as a safely quoted PostgreSQL identifier.
  /// </summary>
  internal string QuotedLockTableName => PgQuoteIdentifier.QuoteIdentifier(LockTableName);
}
