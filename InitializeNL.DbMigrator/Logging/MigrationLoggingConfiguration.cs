using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace InitializeNL.DbMigrator.Logging;

/// <summary>
/// Holds category-specific loggers for the migration library.
/// </summary>
public class MigrationLoggingConfiguration
{
  internal static readonly MigrationLoggingConfiguration NullConfiguration
    = new(NullLoggerFactory.Instance);

  internal MigrationLoggingConfiguration(ILoggerFactory loggerFactory)
  {
    ScriptLogger = loggerFactory.CreateLogger("InitializeNL.DbMigrator.Script");
    MigrationLogger = loggerFactory.CreateLogger("InitializeNL.DbMigrator.Migration");
    LockLogger = loggerFactory.CreateLogger("InitializeNL.DbMigrator.Lock");
    TrackerLogger = loggerFactory.CreateLogger("InitializeNL.DbMigrator.Tracker");
  }

  /// <summary>
  /// Logger for script loading and parsing operations.
  /// </summary>
  internal ILogger ScriptLogger { get; }

  /// <summary>
  /// Logger for migration execution operations.
  /// </summary>
  internal ILogger MigrationLogger { get; }

  /// <summary>
  /// Logger for lock acquisition and release operations.
  /// </summary>
  internal ILogger LockLogger { get; }

  /// <summary>
  /// Logger for migration tracking operations.
  /// </summary>
  internal ILogger TrackerLogger { get; }
}