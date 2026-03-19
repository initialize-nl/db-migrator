using System.Data;
using System.Diagnostics;
using InitializeNL.DbMigrator.Logging;
using InitializeNL.DbMigrator.Scripts;
using Microsoft.Extensions.Logging;

namespace InitializeNL.DbMigrator;

/// <summary>
/// Executes database migrations across multiple databases in parallel.
/// </summary>
public class ParallelMigrationRunner
{
  private readonly IMigrationSource _source;
  private readonly IMigrationLock _lock;
  private readonly IMigrationTracker _tracker;
  private readonly MigrationLoggingConfiguration _logging;
  private readonly int _parallelism;

  internal ParallelMigrationRunner(
    IMigrationSource source,
    IMigrationLock migrationLock,
    IMigrationTracker tracker,
    MigrationLoggingConfiguration logging,
    int parallelism)
  {
    _source = source;
    _lock = migrationLock;
    _tracker = tracker;
    _logging = logging;
    _parallelism = parallelism;
  }

  /// <summary>
  /// Runs migrations on multiple databases in parallel.
  /// </summary>
  /// <param name="connectionFactories">Connection factories for each database to migrate.</param>
  /// <param name="target">Target migration name, or null for latest.</param>
  /// <param name="allowFillGaps">Whether to run skipped migrations.</param>
  /// <param name="ct">Cancellation token.</param>
  /// <returns>Results for each database.</returns>
  public async Task<ParallelMigrationResult> MigrateAsync(
    IEnumerable<IDbConnectionFactory> connectionFactories,
    string? target = null,
    bool allowFillGaps = false,
    CancellationToken ct = default)
  {
    List<IDbConnectionFactory> factories = connectionFactories.ToList();
    _logging.MigrationLogger.LogInformation(
      "Starting parallel migration for {Count} database(s) with parallelism {Parallelism}",
      factories.Count,
      _parallelism);

    using SemaphoreSlim semaphore = new(_parallelism, _parallelism);
    using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

    List<Task<DatabaseMigrationResult>> tasks = factories.Select(async factory =>
    {
      await semaphore.WaitAsync(linkedCts.Token).ConfigureAwait(false);
      try
      {
        if (linkedCts.Token.IsCancellationRequested)
        {
          return new DatabaseMigrationResult(
            factory.ConnectionDescription,
            false,
            "Cancelled",
            [],
            null);
        }

        return await MigrateOneAsync(factory, target, allowFillGaps, linkedCts.Token).ConfigureAwait(false);
      }
      catch (OperationCanceledException)
      {
        return new DatabaseMigrationResult(
          factory.ConnectionDescription,
          false,
          "Cancelled",
          [],
          null);
      }
      catch (Exception ex)
      {
        _logging.MigrationLogger.LogError(ex, "Migration failed for {Database}", factory.ConnectionDescription);
        return new DatabaseMigrationResult(
          factory.ConnectionDescription,
          false,
          ex.Message,
          [],
          ex);
      }
      finally
      {
        semaphore.Release();
      }
    }).ToList();

    DatabaseMigrationResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
    bool allSucceeded = results.All(r => r.Success);

    _logging.MigrationLogger.LogInformation(
      "Parallel migration completed: {Succeeded}/{Total} succeeded",
      results.Count(r => r.Success),
      results.Length);

    return new ParallelMigrationResult(allSucceeded, results);
  }

  /// <summary>
  /// Runs migrations on multiple databases using connection strings.
  /// </summary>
  public async Task<ParallelMigrationResult> MigrateAsync(
    IEnumerable<string> connectionStrings,
    Func<string, IDbConnectionFactory> connectionFactoryCreator,
    string? target = null,
    bool allowFillGaps = false,
    CancellationToken ct = default)
  {
    IEnumerable<IDbConnectionFactory> factories = connectionStrings.Select(connectionFactoryCreator);
    return await MigrateAsync(factories, target, allowFillGaps, ct).ConfigureAwait(false);
  }

  private async Task<DatabaseMigrationResult> MigrateOneAsync(
    IDbConnectionFactory connectionFactory,
    string? target,
    bool allowFillGaps,
    CancellationToken ct)
  {
    string description = connectionFactory.ConnectionDescription;
    _logging.MigrationLogger.LogInformation("Starting migration for {Database}", description);

    ScriptManager scriptManager = new(_logging);
    scriptManager.Load(_source, target);

    using IDbConnection connection = connectionFactory.CreateConnection();
    if (connection.State != ConnectionState.Open)
    {
      connection.Open();
    }

    await _tracker.InitAsync(connection, ct).ConfigureAwait(false);

    _logging.LockLogger.LogDebug("Acquiring lock for {Database}", description);
    bool lockAcquired = await _lock.AcquireAsync(connection, ct).ConfigureAwait(false);
    if (!lockAcquired)
    {
      _logging.LockLogger.LogWarning("Failed to acquire lock for {Database}", description);
      return new DatabaseMigrationResult(description, false, "Failed to acquire lock", [], null);
    }

    try
    {
      IReadOnlyList<string> applied = await _tracker.GetAppliedAsync(connection, ct).ConfigureAwait(false);
      List<Script> queue = await scriptManager.GenerateMigrationQueueAsync(applied, target, allowFillGaps)
        .ConfigureAwait(false);

      if (queue.Count == 0)
      {
        _logging.MigrationLogger.LogInformation("{Database}: Already up to date", description);
        return new DatabaseMigrationResult(description, true, "Already up to date", [], null);
      }

      _logging.MigrationLogger.LogInformation(
        "{Database}: {Count} scripts to execute",
        description,
        queue.Count);

      List<string> executed = [];
      foreach (Script script in queue)
      {
        ct.ThrowIfCancellationRequested();

        _logging.MigrationLogger.LogInformation(
          "{Database}: Executing {Script} ({Type})",
          description,
          script.ShortName,
          script.ScriptType);

        Stopwatch sw = Stopwatch.StartNew();
        await ExecuteScriptAsync(connection, script, ct).ConfigureAwait(false);
        sw.Stop();

        if (script.ScriptType == ScriptType.Up)
        {
          await _tracker.AddAsync(connection, script.ShortName, sw.ElapsedMilliseconds, ct).ConfigureAwait(false);
        }
        else
        {
          await _tracker.RemoveAsync(connection, script.ShortName, sw.ElapsedMilliseconds, ct).ConfigureAwait(false);
        }

        executed.Add(script.ShortName);
      }

      _logging.MigrationLogger.LogInformation("{Database}: Migration completed", description);
      return new DatabaseMigrationResult(description, true, "Migration completed", executed, null);
    }
    finally
    {
      await _lock.ReleaseAsync(connection, ct).ConfigureAwait(false);
      _logging.LockLogger.LogDebug("Released lock for {Database}", description);
    }
  }

  private async Task ExecuteScriptAsync(IDbConnection connection, Script script, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(script.Sql))
    {
      return;
    }

    using IDbCommand command = connection.CreateCommand();
    command.CommandText = script.Sql;
    command.CommandTimeout = 0;

    await Task.Run(() => command.ExecuteNonQuery(), ct).ConfigureAwait(false);
  }
}

/// <summary>
/// Result of a parallel migration run.
/// </summary>
public record ParallelMigrationResult(
  bool AllSucceeded,
  IReadOnlyList<DatabaseMigrationResult> Results)
{
  public IEnumerable<DatabaseMigrationResult> Failed => Results.Where(r => !r.Success);
  public IEnumerable<DatabaseMigrationResult> Succeeded => Results.Where(r => r.Success);
}

/// <summary>
/// Result of migrating a single database.
/// </summary>
public record DatabaseMigrationResult(
  string ConnectionDescription,
  bool Success,
  string Message,
  IReadOnlyList<string> ExecutedMigrations,
  Exception? Error);