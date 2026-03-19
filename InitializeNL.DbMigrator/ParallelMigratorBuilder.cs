using InitializeNL.DbMigrator.Logging;
using Microsoft.Extensions.Logging;

namespace InitializeNL.DbMigrator;

/// <summary>
/// Builder for configuring and creating a parallel migration runner.
/// </summary>
public class ParallelMigratorBuilder
{
  private ILoggerFactory? _loggerFactory;
  private IMigrationSource? _migrationSource;
  private IMigrationLock? _migrationLock;
  private IMigrationTracker? _migrationTracker;
  private int _parallelism = 1;

  /// <summary>
  /// Sets the logger factory.
  /// </summary>
  public ParallelMigratorBuilder UseLoggerFactory(ILoggerFactory? loggerFactory)
  {
    _loggerFactory = loggerFactory;
    return this;
  }

  /// <summary>
  /// Sets the migration source (where to load scripts from).
  /// </summary>
  public ParallelMigratorBuilder UseMigrationSource(IMigrationSource source)
  {
    _migrationSource = source;
    return this;
  }

  /// <summary>
  /// Sets the migration lock implementation.
  /// </summary>
  public ParallelMigratorBuilder UseMigrationLock(IMigrationLock migrationLock)
  {
    _migrationLock = migrationLock;
    return this;
  }

  /// <summary>
  /// Sets the migration tracker implementation.
  /// </summary>
  public ParallelMigratorBuilder UseMigrationTracker(IMigrationTracker tracker)
  {
    _migrationTracker = tracker;
    return this;
  }

  /// <summary>
  /// Disables migration locking.
  /// </summary>
  public ParallelMigratorBuilder DisableLocking()
  {
    _migrationLock = new NoOpMigrationLock();
    return this;
  }

  /// <summary>
  /// Sets the maximum number of databases to migrate in parallel.
  /// </summary>
  public ParallelMigratorBuilder WithParallelism(int parallelism)
  {
    if (parallelism < 1)
    {
      throw new ArgumentOutOfRangeException(nameof(parallelism), "Parallelism must be at least 1");
    }

    _parallelism = parallelism;
    return this;
  }

  /// <summary>
  /// Builds the configured parallel migrator.
  /// </summary>
  public ParallelMigrationRunner Build()
  {
    if (_migrationSource == null)
    {
      throw new InvalidOperationException("Migration source must be configured using UseMigrationSource()");
    }

    if (_migrationTracker == null)
    {
      throw new InvalidOperationException("Migration tracker must be configured using UseMigrationTracker()");
    }

    MigrationLoggingConfiguration loggingConfig = _loggerFactory != null
      ? new MigrationLoggingConfiguration(_loggerFactory)
      : MigrationLoggingConfiguration.NullConfiguration;

    return new ParallelMigrationRunner(
      _migrationSource,
      _migrationLock ?? new NoOpMigrationLock(),
      _migrationTracker,
      loggingConfig,
      _parallelism);
  }
}