using System.Data;

namespace InitializeNL.DbMigrator;

/// <summary>
/// Provides locking mechanism to prevent concurrent migrations.
/// </summary>
public interface IMigrationLock
{
  Task<bool> AcquireAsync(IDbConnection connection, CancellationToken ct = default);
  Task<bool> ReleaseAsync(IDbConnection connection, CancellationToken ct = default);
}

/// <summary>
/// No-op lock implementation for when locking is not needed.
/// </summary>
public class NoOpMigrationLock : IMigrationLock
{
  public Task<bool> AcquireAsync(IDbConnection connection, CancellationToken ct = default)
  {
    return Task.FromResult(true);
  }

  public Task<bool> ReleaseAsync(IDbConnection connection, CancellationToken ct = default)
  {
    return Task.FromResult(true);
  }
}