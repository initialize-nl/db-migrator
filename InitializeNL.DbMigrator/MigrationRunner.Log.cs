using Microsoft.Extensions.Logging;

namespace InitializeNL.DbMigrator;

public partial class MigrationRunner
{
  [LoggerMessage(Level = LogLevel.Information, Message = "Starting migration")]
  private partial void LogStartingMigration();

  [LoggerMessage(Level = LogLevel.Information, Message = "Migration queue: {Count} scripts to execute")]
  private partial void LogMigrationQueue(int count);

  [LoggerMessage(Level = LogLevel.Information, Message = "No migrations to run")]
  private partial void LogNoMigrations();

  [LoggerMessage(Level = LogLevel.Information, Message = "Executing {ScriptName} ({ScriptType})")]
  private partial void LogExecutingScript(string scriptName, string scriptType);

  [LoggerMessage(Level = LogLevel.Information, Message = "Completed {ScriptName} in {Duration}ms")]
  private partial void LogCompletedScript(string scriptName, long duration);

  [LoggerMessage(Level = LogLevel.Information, Message = "Migration completed successfully")]
  private partial void LogMigrationCompleted();

  [LoggerMessage(Level = LogLevel.Error, Message = "Migration failed")]
  private partial void LogMigrationFailed(Exception exception);

  [LoggerMessage(Level = LogLevel.Debug, Message = "Executing code migration {ScriptName}")]
  private partial void LogExecutingCodeMigration(string scriptName);

  [LoggerMessage(Level = LogLevel.Debug, Message = "Completed code migration {ScriptName}")]
  private partial void LogCompletedCodeMigration(string scriptName);

  [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping {ScriptName} - empty script")]
  private partial void LogSkippingEmptyScript(string scriptName);

  [LoggerMessage(Level = LogLevel.Debug, Message = "Executed {ScriptName}, {RowsAffected} rows affected")]
  private partial void LogExecutedScript(string scriptName, int rowsAffected);
}