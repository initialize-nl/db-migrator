namespace InitializeNL.DbMigrator;

/// <summary>
/// Provides migration scripts from a pluggable source.
/// </summary>
public interface IMigrationSource
{
  /// <summary>
  /// Gets all migration scripts (both up and down) from this source.
  /// </summary>
  IEnumerable<Scripts.Script> GetScripts();
}