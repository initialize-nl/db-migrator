using Microsoft.Extensions.Logging;

namespace InitializeNL.DbMigrator;

public partial class ScriptManager
{
  [LoggerMessage(Level = LogLevel.Debug, Message = "Loading scripts from source")]
  private partial void LogLoadingScripts();

  [LoggerMessage(Level = LogLevel.Debug, Message = "Found {Count} script files")]
  private partial void LogFoundScripts(int count);

  [LoggerMessage(Level = LogLevel.Information, Message = "Loaded {Count} migration pairs")]
  private partial void LogLoadedMigrations(int count);

  [LoggerMessage(Level = LogLevel.Error, Message = "Target '{Target}' not found")]
  private partial void LogTargetNotFound(string target);

  [LoggerMessage(Level = LogLevel.Error, Message = "Multiple targets '{Target}' found")]
  private partial void LogMultipleTargets(string target);

  [LoggerMessage(Level = LogLevel.Debug, Message = "Target '{Target}' validated")]
  private partial void LogTargetValidated(string target);

  [LoggerMessage(Level = LogLevel.Debug, Message = "Target migration: {Target}")]
  private partial void LogTargetMigration(string target);

  [LoggerMessage(Level = LogLevel.Debug, Message = "Queued for revert: {Script}")]
  private partial void LogQueuedForRevert(string script);

  [LoggerMessage(Level = LogLevel.Error, Message = "Cannot revert irreversible migrations: {Migrations}")]
  private partial void LogCannotRevertIrreversible(string migrations);

  [LoggerMessage(Level = LogLevel.Error, Message = "Migrations to revert missing scripts: {Migrations}")]
  private partial void LogMissingRevertScripts(string migrations);

  [LoggerMessage(Level = LogLevel.Debug, Message = "Queued for apply: {Script}")]
  private partial void LogQueuedForApply(string script);

  [LoggerMessage(Level = LogLevel.Error, Message = "Gap migrations found: {Migrations}")]
  private partial void LogGapMigrations(string migrations);

  [LoggerMessage(Level = LogLevel.Information, Message = "Generated queue with {Count} scripts")]
  private partial void LogGeneratedQueue(int count);

  [LoggerMessage(Level = LogLevel.Error, Message = "No migration scripts found")]
  private partial void LogNoScriptsFound();

  [LoggerMessage(Level = LogLevel.Warning, Message = "Missing down scripts (treating as irreversible): {Scripts}")]
  private partial void LogMissingDownScripts(string scripts);

  [LoggerMessage(Level = LogLevel.Error, Message = "Missing up scripts: {Scripts}")]
  private partial void LogMissingUpScripts(string scripts);

  [LoggerMessage(Level = LogLevel.Error, Message = "Mismatch in up/down pairs")]
  private partial void LogMismatchPairs();
}