using System.Reflection;
using InitializeNL.DbMigrator.Logging;
using InitializeNL.DbMigrator.Sources;
using Microsoft.Extensions.Logging;

namespace InitializeNL.DbMigrator;

/// <summary>
/// Builder for configuring and creating a migration runner.
/// </summary>
public class MigratorBuilder
{
  private ILoggerFactory? _loggerFactory;
  private readonly List<IMigrationSource> _migrationSources = [];
  private IDbConnectionFactory? _connectionFactory;
  private IMigrationLock? _migrationLock;
  private IMigrationTracker? _migrationTracker;

  /// <summary>
  /// Sets the logger factory for the migrator.
  /// If not set, logging is disabled (silent by default).
  /// </summary>
  public MigratorBuilder UseLoggerFactory(ILoggerFactory? loggerFactory)
  {
    _loggerFactory = loggerFactory;
    return this;
  }

  /// <summary>
  /// Adds a custom migration source.
  /// Can be called multiple times to add multiple sources.
  /// </summary>
  public MigratorBuilder UseMigrationSource(IMigrationSource source)
  {
    _migrationSources.Add(source);
    return this;
  }

  /// <summary>
  /// Adds SQL migrations from embedded resources in the specified assembly and namespace.
  /// </summary>
  public MigratorBuilder UseSqlMigrations(Assembly assembly, string resourceNamespace)
  {
    _migrationSources.Add(new EmbeddedResourceMigrationSource(assembly, resourceNamespace));
    return this;
  }

  /// <summary>
  /// Adds SQL migrations from embedded resources using a marker type.
  /// The assembly and namespace are inferred from the marker type.
  /// </summary>
  public MigratorBuilder UseSqlMigrations(Type markerType)
  {
    _migrationSources.Add(
      new EmbeddedResourceMigrationSource(
        markerType.Assembly,
        markerType.Namespace ?? throw new ArgumentException("Marker type must have a namespace", nameof(markerType))));
    return this;
  }

  /// <summary>
  /// Adds SQL migrations from embedded resources using a marker type.
  /// The assembly and namespace are inferred from the marker type.
  /// </summary>
  public MigratorBuilder UseSqlMigrations<TMarker>()
  {
    return UseSqlMigrations(typeof(TMarker));
  }

  /// <summary>
  /// Adds SQL migrations from a directory on the file system.
  /// </summary>
  public MigratorBuilder UseSqlMigrationsFromDirectory(string path)
  {
    _migrationSources.Add(new FileSystemMigrationSource(path));
    return this;
  }

  /// <summary>
  /// Adds code migrations from the specified assembly.
  /// Scans for classes decorated with [Migration] that inherit from CodeMigration.
  /// </summary>
  public MigratorBuilder UseCodeMigrations(Assembly assembly, string? namespaceFilter = null)
  {
    _migrationSources.Add(new CodeMigrationSource(assembly, namespaceFilter));
    return this;
  }

  /// <summary>
  /// Adds code migrations using a marker type.
  /// Scans the marker type's assembly for classes in the same namespace (or below).
  /// </summary>
  public MigratorBuilder UseCodeMigrations(Type markerType)
  {
    _migrationSources.Add(new CodeMigrationSource(markerType.Assembly, markerType.Namespace));
    return this;
  }

  /// <summary>
  /// Adds code migrations using a marker type.
  /// Scans the marker type's assembly for classes in the same namespace (or below).
  /// </summary>
  public MigratorBuilder UseCodeMigrations<TMarker>()
  {
    return UseCodeMigrations(typeof(TMarker));
  }

  /// <summary>
  /// Sets the connection factory for database connections.
  /// </summary>
  public MigratorBuilder UseConnectionFactory(IDbConnectionFactory factory)
  {
    _connectionFactory = factory;
    return this;
  }

  /// <summary>
  /// Sets the migration lock implementation.
  /// </summary>
  public MigratorBuilder UseMigrationLock(IMigrationLock migrationLock)
  {
    _migrationLock = migrationLock;
    return this;
  }

  /// <summary>
  /// Sets the migration tracker implementation.
  /// </summary>
  public MigratorBuilder UseMigrationTracker(IMigrationTracker tracker)
  {
    _migrationTracker = tracker;
    return this;
  }

  /// <summary>
  /// Disables migration locking.
  /// </summary>
  public MigratorBuilder DisableLocking()
  {
    _migrationLock = new NoOpMigrationLock();
    return this;
  }

  /// <summary>
  /// Builds the configured migrator.
  /// </summary>
  public MigrationRunner Build()
  {
    if (_migrationSources.Count == 0)
    {
      throw new InvalidOperationException(
        "At least one migration source must be configured using UseSqlMigrations(), UseCodeMigrations(), or UseMigrationSource()");
    }

    if (_connectionFactory == null)
    {
      throw new InvalidOperationException("Connection factory must be configured using UseConnectionFactory()");
    }

    if (_migrationTracker == null)
    {
      throw new InvalidOperationException("Migration tracker must be configured using UseMigrationTracker()");
    }

    IMigrationSource combinedSource = _migrationSources.Count == 1
      ? _migrationSources[0]
      : new CompositeMigrationSource(_migrationSources);

    MigrationLoggingConfiguration loggingConfig = _loggerFactory != null
      ? new MigrationLoggingConfiguration(_loggerFactory)
      : MigrationLoggingConfiguration.NullConfiguration;

    return new MigrationRunner(
      combinedSource,
      _connectionFactory,
      _migrationLock ?? new NoOpMigrationLock(),
      _migrationTracker,
      loggingConfig);
  }
}