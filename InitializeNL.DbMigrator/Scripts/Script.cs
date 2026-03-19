using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace InitializeNL.DbMigrator.Scripts;

public partial class Script
{
  private static readonly Regex DateTimePattern = DateTimeRegex();
  private static readonly Regex ArgPattern = ArgRegex();

  public string ShortName { get; }
  public ScriptType ScriptType { get; }
  public string Sql { get; }
  public string SourceName { get; }
  public int RepeatUntilAffectedLessThanOrEqual { get; private set; } = int.MaxValue;
  public bool DeferAddToHistory { get; private set; }

  /// <summary>
  /// Indicates that this migration cannot be rolled back.
  /// Set via "-- arg: irreversible" in SQL or Irreversible=true in [Migration] attribute.
  /// </summary>
  public bool Irreversible { get; private set; }

  /// <summary>
  /// Indicates that this script contains destructive actions (DROP, DELETE, TRUNCATE, etc.).
  /// Set via "-- arg: destructive" in SQL or Destructive=true in [Migration] attribute.
  /// </summary>
  public bool Destructive { get; private set; }

  /// <summary>
  /// Marks this script as irreversible. Used when no down script is found.
  /// </summary>
  internal void MarkAsIrreversible()
  {
    Irreversible = true;
  }

  /// <summary>
  /// Optional code executor for code-based migrations.
  /// When set, this is executed instead of the SQL.
  /// </summary>
  public Func<IDbConnection, ILogger, CancellationToken, Task>? CodeExecutor { get; }

  /// <summary>
  /// Indicates whether this is a code migration (vs SQL migration).
  /// </summary>
  public bool IsCodeMigration => CodeExecutor != null;

  /// <summary>
  /// Creates a script from SQL content.
  /// </summary>
  /// <param name="name">The filename (e.g., "001_CreateUsers.up.sql").</param>
  /// <param name="type">The script type (Up or Down).</param>
  /// <param name="sqlContent">The SQL content.</param>
  /// <param name="sourceName">Optional source identifier for error messages.</param>
  public Script(string name, ScriptType type, string sqlContent, string? sourceName = null)
  {
    SourceName = sourceName ?? name;
    ShortName = name.Replace(type.GetFileExtension(), "", StringComparison.OrdinalIgnoreCase);
    ScriptType = type;
    Sql = sqlContent;
    CodeExecutor = null;

    ParseArguments();
  }

  /// <summary>
  /// Creates a script from a code executor.
  /// </summary>
  /// <param name="name">The migration name (e.g., "2024-01-15_00-00-00Z_transform_data").</param>
  /// <param name="type">The script type (Up or Down).</param>
  /// <param name="codeExecutor">The code to execute.</param>
  /// <param name="sourceName">Optional source identifier for error messages.</param>
  /// <param name="irreversible">If true, this migration cannot be rolled back.</param>
  public Script(
    string name,
    ScriptType type,
    Func<IDbConnection, ILogger, CancellationToken, Task> codeExecutor,
    string? sourceName = null,
    bool irreversible = false,
    bool destructive = false)
  {
    SourceName = sourceName ?? name;
    ShortName = name;
    ScriptType = type;
    Sql = string.Empty;
    CodeExecutor = codeExecutor;
    Irreversible = irreversible;
    Destructive = destructive;
  }

  /// <summary>
  /// Creates a script by reading from a file path.
  /// </summary>
  public static Script FromFile(string filePath, ScriptType type)
  {
    string content = File.ReadAllText(filePath);
    return new Script(Path.GetFileName(filePath), type, content, filePath);
  }

  private void ParseArguments()
  {
    Dictionary<string, string[]> sqlArgs = ArgPattern.Matches(Sql)
      .Select(m => m.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
      .ToDictionary(v => v[0], v => v[1..]);

    foreach (KeyValuePair<string, string[]> arg in sqlArgs)
    {
      switch (arg.Key)
      {
        case "repeat-until-affected-lte":
          try
          {
            RepeatUntilAffectedLessThanOrEqual = Convert.ToInt32(arg.Value[0], CultureInfo.InvariantCulture);
            DeferAddToHistory = true;
          }
          catch
          {
            throw new InvalidOperationException(
              $"Argument repeat-until-affected-lte in {SourceName} expects exactly one number.");
          }

          break;
        case "irreversible":
          Irreversible = true;
          break;
        case "destructive":
          Destructive = true;
          break;
        default:
          throw new InvalidOperationException($"Unrecognized argument {arg.Key} in {SourceName}");
      }
    }
  }

  [GeneratedRegex(@"^(?<Year>\d{4})-(?<Month>\d{2})-(?<Day>\d{2})_(?<Hour>\d{2})-(?<Minute>\d{2})-(?<Second>\d{2})Z")]
  private static partial Regex DateTimeRegex();

  [GeneratedRegex(@"^\s*-- arg:\s*(.*?)\s*$", RegexOptions.Multiline)]
  private static partial Regex ArgRegex();
}