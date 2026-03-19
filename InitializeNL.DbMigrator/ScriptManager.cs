using InitializeNL.DbMigrator.Logging;
using InitializeNL.DbMigrator.Scripts;
using Microsoft.Extensions.Logging;

namespace InitializeNL.DbMigrator;

/// <summary>
/// Manages loading and pairing of migration scripts.
/// </summary>
public partial class ScriptManager
{
  private readonly MigrationLoggingConfiguration _logging;
  private readonly ILogger _logger;
  private List<UpDownScript> _migrations = [];

  public ScriptManager() : this(MigrationLoggingConfiguration.NullConfiguration)
  {
  }

  internal ScriptManager(MigrationLoggingConfiguration logging)
  {
    _logging = logging;
    _logger = logging.ScriptLogger;
  }

  public IReadOnlyList<UpDownScript> Migrations => _migrations;

  /// <summary>
  /// Loads migrations from a source and pairs up/down scripts.
  /// </summary>
  public void Load(IMigrationSource source, string? target = null)
  {
    LogLoadingScripts();

    List<Script> scripts = [.. source.GetScripts()];
    LogFoundScripts(scripts.Count);

    _migrations = PairUpDownScripts(scripts);
    LogLoadedMigrations(_migrations.Count);

    if (target != null)
    {
      int count = _migrations.Count(s => s.ShortName.Equals(target, StringComparison.OrdinalIgnoreCase));
      if (count <= 0)
      {
        LogTargetNotFound(target);
        throw new InvalidOperationException($"Target '{target}' not found!");
      }

      if (count > 1)
      {
        LogMultipleTargets(target);
        throw new InvalidOperationException($"More than one target '{target}' found!");
      }

      LogTargetValidated(target);
    }
  }

  /// <summary>
  /// Generates the queue of scripts to run to reach the target migration.
  /// </summary>
  public async Task<List<Script>> GenerateMigrationQueueAsync(
    IReadOnlyList<string> appliedMigrations,
    string? targetName,
    bool allowFillGaps)
  {
    List<string> shortNamesDone = [.. appliedMigrations.OrderByDescending(s => s)];

    if (targetName == null)
    {
      targetName = shortNamesDone.Count == 0 || string.Compare(
        _migrations[^1].ShortName,
        shortNamesDone[0],
        StringComparison.OrdinalIgnoreCase) > 0
        ? _migrations[^1].ShortName
        : shortNamesDone[0];
    }

    if (targetName == null)
    {
      throw new InvalidOperationException("Could not determine target.");
    }

    LogTargetMigration(targetName);

    List<Script> migrationQueue = [];

    // Revert all migrations (backwards order) that are applied and after target
    List<string> revertMigrationsWithoutScript = [];
    List<string> irreversibleMigrations = [];
    migrationQueue.AddRange(
      shortNamesDone
        .TakeWhile(s => string.Compare(s, targetName, StringComparison.OrdinalIgnoreCase) > 0)
        .Select(s =>
        {
          UpDownScript? script = _migrations.Find(scriptFile =>
                                                    string.Equals(
                                                      s,
                                                      scriptFile.ShortName,
                                                      StringComparison.OrdinalIgnoreCase));
          if (script == null)
          {
            revertMigrationsWithoutScript.Add(s);
            return null!;
          }

          if (script.Irreversible)
          {
            irreversibleMigrations.Add(s);
            return null!;
          }

          LogQueuedForRevert(s);
          return script.Down!;
        }));

    if (irreversibleMigrations.Count > 0)
    {
      LogCannotRevertIrreversible(string.Join(", ", irreversibleMigrations));
      throw new InvalidOperationException(
        $"Cannot revert irreversible migrations: {string.Join(", ", irreversibleMigrations)}");
    }

    if (revertMigrationsWithoutScript.Count > 0)
    {
      LogMissingRevertScripts(string.Join(", ", revertMigrationsWithoutScript));
      throw new InvalidOperationException(
        $"To be reverted migration without script found: {string.Join(", ", revertMigrationsWithoutScript)}");
    }

    // Apply all migrations that are not applied and before or equal to target
    List<string> forwardMigrationsIntermediate = [];
    migrationQueue.AddRange(
      _migrations
        .TakeWhile(s => string.Compare(s.ShortName, targetName, StringComparison.OrdinalIgnoreCase) <= 0)
        .Where(s => !shortNamesDone.Contains(s.ShortName))
        .Select(s =>
        {
          if (shortNamesDone.Count > 0 && string.Compare(
                s.ShortName,
                shortNamesDone.First(),
                StringComparison.OrdinalIgnoreCase) < 0)
          {
            forwardMigrationsIntermediate.Add(s.ShortName);
          }

          LogQueuedForApply(s.ShortName);
          return s.Up;
        }));

    if (!allowFillGaps && forwardMigrationsIntermediate.Count > 0)
    {
      LogGapMigrations(string.Join(", ", forwardMigrationsIntermediate));
      throw new InvalidOperationException(
        $"Migrations before the last executed migration found: {string.Join(", ", forwardMigrationsIntermediate)}. Manually revert first or allow fill gaps.");
    }

    LogGeneratedQueue(migrationQueue.Count);
    return await Task.FromResult(migrationQueue);
  }

  private List<UpDownScript> PairUpDownScripts(List<Script> scripts)
  {
    if (scripts.Count == 0)
    {
      LogNoScriptsFound();
      throw new InvalidOperationException("No migration scripts found.");
    }

    List<UpDownScript> result =
    [
      .. scripts
        .Where(s => s.ScriptType == ScriptType.Up)
        .Select(upScript => new UpDownScript
        {
          Up = upScript,
          Down = scripts.FirstOrDefault(downScript =>
                                          downScript.ScriptType == ScriptType.Down
                                          && downScript.ShortName == upScript.ShortName),
          ShortName = upScript.ShortName,
        })
        .OrderBy(s => s.ShortName),
    ];

    // Warn about missing down scripts for migrations not explicitly marked irreversible
    List<UpDownScript> missingDown = result
      .Where(r => r.Down == null && !r.Irreversible)
      .ToList();
    if (missingDown.Count > 0)
    {
      LogMissingDownScripts(string.Join(", ", missingDown.Select(r => r.Up.SourceName)));

      // Mark these as irreversible implicitly
      foreach (UpDownScript script in missingDown)
      {
        script.Up.MarkAsIrreversible();
      }
    }

    List<string> missingUp = scripts
      .Where(s => s.ScriptType == ScriptType.Down && result.All(r => r.Down != s))
      .Select(r => r.SourceName)
      .ToList();
    if (missingUp.Count > 0)
    {
      LogMissingUpScripts(string.Join(", ", missingUp));
      throw new InvalidOperationException($"Down scripts without up scripts found: {string.Join(", ", missingUp)}");
    }

    // Count expected scripts: migrations with down scripts have 2, those without have 1
    int expectedScriptCount = result.Sum(r => r.Down != null ? 2 : 1);
    if (expectedScriptCount != scripts.Count)
    {
      LogMismatchPairs();
      throw new InvalidOperationException("Mismatch in up/down pairs.");
    }

    return result;
  }
}