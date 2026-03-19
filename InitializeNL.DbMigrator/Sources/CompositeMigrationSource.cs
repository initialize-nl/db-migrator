namespace InitializeNL.DbMigrator.Sources;

/// <summary>
/// Combines multiple migration sources into one.
/// Scripts from all sources are merged and returned together.
/// </summary>
public class CompositeMigrationSource : IMigrationSource
{
  private readonly IReadOnlyList<IMigrationSource> _sources;

  public CompositeMigrationSource(IEnumerable<IMigrationSource> sources)
  {
    _sources = sources.ToList();
  }

  public CompositeMigrationSource(params IMigrationSource[] sources)
  {
    _sources = sources;
  }

  public IEnumerable<Scripts.Script> GetScripts()
  {
    return _sources.SelectMany(s => s.GetScripts());
  }
}