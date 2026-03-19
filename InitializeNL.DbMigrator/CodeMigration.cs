using System.Data;
using Microsoft.Extensions.Logging;

namespace InitializeNL.DbMigrator;

/// <summary>
/// Base class for code-based migrations.
/// Inherit from this class and decorate with [Migration("name")] to create a code migration.
/// </summary>
public abstract class CodeMigration
{
  /// <summary>
  /// Executes the forward migration.
  /// </summary>
  /// <param name="connection">The database connection.</param>
  /// <param name="logger">Logger for migration progress.</param>
  /// <param name="ct">Cancellation token.</param>
  public abstract Task UpAsync(IDbConnection connection, ILogger logger, CancellationToken ct);

  /// <summary>
  /// Executes the rollback migration. Override if rollback is supported.
  /// </summary>
  /// <param name="connection">The database connection.</param>
  /// <param name="logger">Logger for migration progress.</param>
  /// <param name="ct">Cancellation token.</param>
  public virtual Task DownAsync(IDbConnection connection, ILogger logger, CancellationToken ct)
  {
    return Task.CompletedTask;
  }
}