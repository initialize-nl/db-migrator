using System.Reflection;
using InitializeNL.DbMigrator.Scripts;

namespace InitializeNL.DbMigrator.Sources;

/// <summary>
/// Discovers code migrations by scanning an assembly for classes
/// decorated with [Migration] that inherit from CodeMigration.
/// </summary>
public class CodeMigrationSource : IMigrationSource
{
  private readonly Assembly _assembly;
  private readonly string? _namespaceFilter;

  /// <summary>
  /// Creates a new code migration source.
  /// </summary>
  /// <param name="assembly">The assembly to scan for code migrations.</param>
  /// <param name="namespaceFilter">Optional namespace prefix to filter types.</param>
  public CodeMigrationSource(Assembly assembly, string? namespaceFilter = null)
  {
    _assembly = assembly;
    _namespaceFilter = namespaceFilter;
  }

  public IEnumerable<Script> GetScripts()
  {
    IEnumerable<Type> migrationTypes = _assembly.GetTypes()
      .Where(t => t.IsClass && !t.IsAbstract)
      .Where(t => t.IsSubclassOf(typeof(CodeMigration)))
      .Where(t => t.GetCustomAttribute<MigrationAttribute>() != null)
      .Where(t => _namespaceFilter == null ||
                  (t.Namespace?.StartsWith(_namespaceFilter, StringComparison.Ordinal) ?? false));

    foreach (Type type in migrationTypes)
    {
      MigrationAttribute attribute = type.GetCustomAttribute<MigrationAttribute>()!;
      string migrationName = attribute.Name;
      bool irreversible = attribute.Irreversible;

      CodeMigration instance = (CodeMigration) Activator.CreateInstance(type)!;

      // Create Up script
      yield return new Script(
        migrationName,
        ScriptType.Up,
        (conn, logger, ct) => instance.UpAsync(conn, logger, ct),
        $"{type.FullName}.UpAsync",
        irreversible,
        attribute.Destructive);

      // Create Down script only if not irreversible
      if (!irreversible)
      {
        yield return new Script(
          migrationName,
          ScriptType.Down,
          (conn, logger, ct) => instance.DownAsync(conn, logger, ct),
          $"{type.FullName}.DownAsync",
          destructive: attribute.Destructive);
      }
    }
  }
}