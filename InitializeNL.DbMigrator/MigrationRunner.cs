using System.Data;
using System.Diagnostics;
using InitializeNL.DbMigrator.Logging;
using InitializeNL.DbMigrator.Scripts;
using Microsoft.Extensions.Logging;

namespace InitializeNL.DbMigrator;

/// <summary>
/// Executes database migrations.
/// </summary>
public partial class MigrationRunner
{
  private readonly IMigrationSource _source;
  private readonly IDbConnectionFactory _connectionFactory;
  private readonly IMigrationLock _lock;
  private readonly IMigrationTracker _tracker;
  private readonly MigrationLoggingConfiguration _logging;
  private readonly ILogger _logger;
  private readonly ScriptManager _scriptManager;

  internal MigrationRunner(
    IMigrationSource source,
    IDbConnectionFactory connectionFactory,
    IMigrationLock migrationLock,
    IMigrationTracker tracker,
    MigrationLoggingConfiguration logging)
  {
    _source = source;
    _connectionFactory = connectionFactory;
    _lock = migrationLock;
    _tracker = tracker;
    _logging = logging;
    _logger = logging.MigrationLogger;
    _scriptManager = new ScriptManager(logging);
  }

  /// <summary>
  /// Runs migrations to the specified target (or latest if null).
  /// </summary>
  public async Task<MigrationResult> MigrateAsync(
    string? target = null,
    bool allowFillGaps = false,
    CancellationToken ct = default)
  {
    LogStartingMigration();

    // Load scripts
    _scriptManager.Load(_source, target);
    _logging.ScriptLogger.LogDebug("Loaded {Count} migrations", _scriptManager.Migrations.Count);

    using IDbConnection connection = _connectionFactory.CreateConnection();
    if (connection.State != ConnectionState.Open)
    {
      connection.Open();
    }

    // Initialize tracker
    await _tracker.InitAsync(connection, ct);
    _logging.TrackerLogger.LogDebug("Migration tracker initialized");

    // Acquire lock
    _logging.LockLogger.LogDebug("Acquiring migration lock");
    bool lockAcquired = await _lock.AcquireAsync(connection, ct);
    if (!lockAcquired)
    {
      _logging.LockLogger.LogWarning("Failed to acquire migration lock");
      return new MigrationResult(false, "Failed to acquire migration lock", []);
    }

    _logging.LockLogger.LogDebug("Migration lock acquired");

    try
    {
      // Get applied migrations
      IReadOnlyList<string> applied = await _tracker.GetAppliedAsync(connection, ct);
      _logging.TrackerLogger.LogDebug("Found {Count} applied migrations", applied.Count);

      // Generate queue
      List<Script> queue = await _scriptManager.GenerateMigrationQueueAsync(applied, target, allowFillGaps);
      LogMigrationQueue(queue.Count);

      if (queue.Count == 0)
      {
        LogNoMigrations();
        return new MigrationResult(true, "Already up to date", []);
      }

      // Execute migrations
      List<string> executed = [];
      foreach (Script script in queue)
      {
        ct.ThrowIfCancellationRequested();

        LogExecutingScript(script.ShortName, script.ScriptType.ToString());

        Stopwatch sw = Stopwatch.StartNew();
        await ExecuteScriptAsync(connection, script, ct);
        sw.Stop();

        if (script.ScriptType == ScriptType.Up)
        {
          await _tracker.AddAsync(connection, script.ShortName, sw.ElapsedMilliseconds, ct);
          _logging.TrackerLogger.LogDebug(
            "Added {Migration} to history ({Duration}ms)",
            script.ShortName,
            sw.ElapsedMilliseconds);
        }
        else
        {
          await _tracker.RemoveAsync(connection, script.ShortName, sw.ElapsedMilliseconds, ct);
          _logging.TrackerLogger.LogDebug(
            "Removed {Migration} from history ({Duration}ms)",
            script.ShortName,
            sw.ElapsedMilliseconds);
        }

        executed.Add(script.ShortName);
        LogCompletedScript(script.ShortName, sw.ElapsedMilliseconds);
      }

      LogMigrationCompleted();
      return new MigrationResult(true, "Migration completed", executed);
    }
    catch (Exception ex)
    {
      LogMigrationFailed(ex);
      throw;
    }
    finally
    {
      _logging.LockLogger.LogDebug("Releasing migration lock");
      await _lock.ReleaseAsync(connection, ct);
      _logging.LockLogger.LogDebug("Migration lock released");
    }
  }

  /// <summary>
  /// Gets the current migration status without running any migrations.
  /// </summary>
  public async Task<MigrationStatus> GetStatusAsync(CancellationToken ct = default)
  {
    _scriptManager.Load(_source, null);

    using IDbConnection connection = _connectionFactory.CreateConnection();
    if (connection.State != ConnectionState.Open)
    {
      connection.Open();
    }

    await _tracker.InitAsync(connection, ct).ConfigureAwait(false);
    IReadOnlyList<string> applied = await _tracker.GetAppliedAsync(connection, ct).ConfigureAwait(false);

    List<string> available = _scriptManager.Migrations.Select(m => m.ShortName).ToList();
    List<string> pending = available.Except(applied).ToList();

    return new MigrationStatus(
      applied.OrderBy(x => x).ToList(),
      pending.OrderBy(x => x).ToList(),
      available.OrderBy(x => x).ToList());
  }

  private async Task ExecuteScriptAsync(IDbConnection connection, Script script, CancellationToken ct)
  {
    // Code migration
    if (script.IsCodeMigration)
    {
      LogExecutingCodeMigration(script.ShortName);
      await script.CodeExecutor!(connection, _logging.MigrationLogger, ct);
      LogCompletedCodeMigration(script.ShortName);
      return;
    }

    // SQL migration
    if (string.IsNullOrWhiteSpace(script.Sql))
    {
      LogSkippingEmptyScript(script.ShortName);
      return;
    }

    using IDbCommand command = connection.CreateCommand();
    command.CommandText = script.Sql;
    command.CommandTimeout = 0; // No timeout for migrations

    int affected = await Task.Run(() => command.ExecuteNonQuery(), ct);
    LogExecutedScript(script.ShortName, affected);
  }
}

/// <summary>
/// Result of a migration run.
/// </summary>
public record MigrationResult(
  bool Success,
  string Message,
  IReadOnlyList<string> ExecutedMigrations
);

/// <summary>
/// Status of migrations for a database.
/// </summary>
public record MigrationStatus(
  IReadOnlyList<string> Applied,
  IReadOnlyList<string> Pending,
  IReadOnlyList<string> Available
);