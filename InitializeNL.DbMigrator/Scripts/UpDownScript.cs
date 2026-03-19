namespace InitializeNL.DbMigrator.Scripts;

public class UpDownScript
{
  public required Script Up { get; init; }

  /// <summary>
  /// The down (rollback) script. Null if the migration is marked as irreversible.
  /// </summary>
  public Script? Down { get; init; }

  public required string ShortName { get; init; }

  /// <summary>
  /// True if this migration cannot be rolled back.
  /// </summary>
  public bool Irreversible => Up.Irreversible;
}