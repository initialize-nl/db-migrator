namespace InitializeNL.DbMigrator;

/// <summary>
/// Represents a target database for migration.
/// </summary>
public record MigrationTarget
{
  /// <summary>
  /// The connection string for this target.
  /// </summary>
  public required string ConnectionString { get; init; }

  /// <summary>
  /// The server hostname.
  /// </summary>
  public required string Server { get; init; }

  /// <summary>
  /// The database name.
  /// </summary>
  public required string Database { get; init; }

  /// <summary>
  /// Connection string with password masked for safe logging.
  /// </summary>
  public required string SafeConnectionString { get; init; }
}

/// <summary>
/// Groups migration targets by server.
/// </summary>
public record MigrationTargetGroup(string Server, IReadOnlyList<MigrationTarget> Targets);

/// <summary>
/// Provides migration targets (database connections) for the migration runner.
/// </summary>
public interface IConnectionProvider
{
  /// <summary>
  /// Discovers and returns all migration targets, grouped by server.
  /// </summary>
  Task<IReadOnlyList<MigrationTargetGroup>> GetTargetsAsync(CancellationToken ct = default);
}