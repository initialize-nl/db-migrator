using Npgsql;

namespace InitializeNL.DbMigrator.Postgres;

/// <summary>
/// Provides a single connection target from a connection string.
/// </summary>
public class SingleConnectionProvider : IConnectionProvider
{
  private readonly string _connectionString;

  public SingleConnectionProvider(string connectionString)
  {
    _connectionString = connectionString;
  }

  public Task<IReadOnlyList<MigrationTargetGroup>> GetTargetsAsync(CancellationToken ct = default)
  {
    NpgsqlConnectionStringBuilder builder = new(_connectionString)
    {
      IncludeErrorDetail = true,
    };
    string connectionString = builder.ToString();
    string server = builder.Host ?? string.Empty;
    string database = builder.Database ?? string.Empty;

    builder.Password = "*****";
    builder.Passfile = null;
    string safeConnectionString = builder.ToString();

    MigrationTarget target = new()
    {
      ConnectionString = connectionString,
      Server = server,
      Database = database,
      SafeConnectionString = safeConnectionString,
    };

    IReadOnlyList<MigrationTargetGroup> result = [new(server, [target])];
    return Task.FromResult(result);
  }
}