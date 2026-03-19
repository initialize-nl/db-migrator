using System.Reflection;
using InitializeNL.DbMigrator.Scripts;

namespace InitializeNL.DbMigrator.Sources;

/// <summary>
/// Loads migration scripts from embedded resources in an assembly.
/// </summary>
public class EmbeddedResourceMigrationSource(Assembly assembly, string resourceNamespace) : IMigrationSource
{
  private readonly string _resourceNamespace =
    resourceNamespace.EndsWith('.') ? resourceNamespace : resourceNamespace + ".";

  /// <summary>
  /// Creates a source from embedded resources in the specified assembly.
  /// </summary>
  public static EmbeddedResourceMigrationSource FromAssembly(Assembly assembly, string resourceNamespace)
  {
    return new EmbeddedResourceMigrationSource(assembly, resourceNamespace);
  }

  /// <summary>
  /// Creates a source from embedded resources in the assembly containing the specified type.
  /// </summary>
  public static EmbeddedResourceMigrationSource FromAssemblyOf<T>(string resourceNamespace)
  {
    return new EmbeddedResourceMigrationSource(typeof(T).Assembly, resourceNamespace);
  }

  public IEnumerable<Script> GetScripts()
  {
    string[] resourceNames = assembly.GetManifestResourceNames()
      .Where(r => r.StartsWith(_resourceNamespace, StringComparison.OrdinalIgnoreCase))
      .ToArray();

    foreach (string resourceName in resourceNames)
    {
      string fileName = resourceName[_resourceNamespace.Length..];

      ScriptType? scriptType = null;
      if (fileName.EndsWith(ScriptType.Up.GetFileExtension(), StringComparison.OrdinalIgnoreCase))
      {
        scriptType = ScriptType.Up;
      }
      else if (fileName.EndsWith(ScriptType.Down.GetFileExtension(), StringComparison.OrdinalIgnoreCase))
      {
        scriptType = ScriptType.Down;
      }

      if (scriptType == null)
      {
        continue;
      }

      string content = ReadResource(resourceName);
      yield return new Script(fileName, scriptType.Value, content, $"embedded://{resourceName}");
    }
  }

  private string ReadResource(string resourceName)
  {
    using Stream stream = assembly.GetManifestResourceStream(resourceName)
                          ?? throw new InvalidOperationException($"Could not read embedded resource: {resourceName}");
    using StreamReader reader = new(stream);
    return reader.ReadToEnd();
  }
}