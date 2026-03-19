using System.Data;

namespace InitializeNL.DbMigrator;

/// <summary>
/// Tracks which migrations have been applied to the database.
/// </summary>
public interface IMigrationTracker
{
  /// <summary>
  /// Ensures the migration tracking tables exist.
  /// </summary>
  Task InitAsync(IDbConnection connection, CancellationToken ct = default);

  /// <summary>
  /// Gets the list of applied migration IDs.
  /// </summary>
  Task<IReadOnlyList<string>> GetAppliedAsync(IDbConnection connection, CancellationToken ct = default);

  /// <summary>
  /// Records a migration as applied.
  /// </summary>
  Task AddAsync(IDbConnection connection, string migrationId, long? durationMs = null, CancellationToken ct = default);

  /// <summary>
  /// Removes a migration from the applied list (for rollback).
  /// </summary>
  Task RemoveAsync(
    IDbConnection connection,
    string migrationId,
    long? durationMs = null,
    CancellationToken ct = default);
}