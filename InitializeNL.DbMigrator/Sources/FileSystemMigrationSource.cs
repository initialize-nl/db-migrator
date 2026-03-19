using InitializeNL.DbMigrator.Scripts;

namespace InitializeNL.DbMigrator.Sources;

/// <summary>
/// Loads migration scripts from a filesystem directory.
/// </summary>
public class FileSystemMigrationSource(string scriptsDirectory) : IMigrationSource
{
  private readonly string _scriptsDirectory = scriptsDirectory
    .Replace(
      Path.AltDirectorySeparatorChar.ToString(),
      Path.DirectorySeparatorChar.ToString(),
      StringComparison.Ordinal);

  public IEnumerable<Script> GetScripts()
  {
    DirectoryInfo dir = new(_scriptsDirectory);

    if (!dir.Exists)
    {
      throw new DirectoryNotFoundException($"Scripts directory not found: {_scriptsDirectory}");
    }

    foreach (FileInfo file in dir.GetFiles($"*{ScriptType.Up.GetFileExtension()}"))
    {
      yield return Script.FromFile(file.FullName, ScriptType.Up);
    }

    foreach (FileInfo file in dir.GetFiles($"*{ScriptType.Down.GetFileExtension()}"))
    {
      yield return Script.FromFile(file.FullName, ScriptType.Down);
    }
  }
}