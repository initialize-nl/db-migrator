namespace InitializeNL.DbMigrator;

/// <summary>
/// Marks a class as a code migration with the specified name.
/// The name should follow the migration naming convention: YYYY-MM-DD_HH-mm-ssZ_description
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MigrationAttribute : Attribute
{
  public string Name { get; }

  /// <summary>
  /// When true, this migration cannot be rolled back and no Down method is required.
  /// </summary>
  public bool Irreversible { get; set; }

  /// <summary>
  /// When true, this migration contains destructive actions that require explicit confirmation.
  /// </summary>
  public bool Destructive { get; set; }

  public MigrationAttribute(string name)
  {
    Name = name;
  }
}