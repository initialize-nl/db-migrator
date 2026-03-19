namespace InitializeNL.DbMigrator.Scripts;

public enum ScriptType
{
  Up,
  Down,
}

public static class ScriptTypeExtensions
{
  public static string GetFileExtension(this ScriptType type)
  {
    return type switch
    {
      ScriptType.Up => ".up.sql",
      ScriptType.Down => ".down.sql",
      _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unrecognized type"),
    };
  }
}