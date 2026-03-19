using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Dapper;
using Npgsql;

namespace InitializeNL.DbMigrator.Postgres;

/// <summary>
/// Discovers migration targets by executing a SQL query against a central database.
/// The query must return rows with "server" and "connection_string" columns.
/// </summary>
public class SqlDiscoveryConnectionProvider : IConnectionProvider
{
  private readonly string _connectionString;
  private readonly string _discoverySql;

  /// <summary>
  /// Creates a provider that discovers targets by executing a SQL query.
  /// </summary>
  /// <param name="connectionString">Connection string to the central/master database.</param>
  /// <param name="discoverySql">SQL query that returns rows with "server" and "connection_string" columns.</param>
  public SqlDiscoveryConnectionProvider(string connectionString, string discoverySql)
  {
    _connectionString = connectionString;
    _discoverySql = discoverySql;
  }

  /// <summary>
  /// Creates a provider that discovers targets by executing a SQL file.
  /// </summary>
  /// <param name="connectionString">Connection string to the central/master database.</param>
  /// <param name="sqlFilePath">Path to a SQL file that returns rows with "server" and "connection_string" columns.</param>
  public static async Task<SqlDiscoveryConnectionProvider> FromFileAsync(string connectionString, string sqlFilePath)
  {
    string sql = await File.ReadAllTextAsync(sqlFilePath).ConfigureAwait(false);
    return new SqlDiscoveryConnectionProvider(connectionString, sql);
  }

  public async Task<IReadOnlyList<MigrationTargetGroup>> GetTargetsAsync(CancellationToken ct = default)
  {
#pragma warning disable CA2007
    await using NpgsqlConnection connection = new(_connectionString);
#pragma warning restore CA2007
    await connection.OpenAsync(ct).ConfigureAwait(false);

    IEnumerable<DiscoveryResult> rows =
      await connection.QueryAsync<DiscoveryResult>(_discoverySql).ConfigureAwait(false);

    List<MigrationTargetGroup> groups = rows
      .Select(row =>
      {
        NpgsqlConnectionStringBuilder builder = new(row.ConnectionString)
        {
          IncludeErrorDetail = true,
        };
        string connStr = builder.ToString();
        string server = builder.Host ?? row.Server;
        string database = builder.Database ?? string.Empty;

        builder.Password = "*****";
        builder.Passfile = null;
        string safeConnStr = builder.ToString();

        return new MigrationTarget
        {
          ConnectionString = connStr,
          Server = server,
          Database = database,
          SafeConnectionString = safeConnStr,
        };
      })
      .GroupBy(t => t.Server)
      .Select(g => new MigrationTargetGroup(g.Key, g.ToList()))
      .ToList();

    return groups;
  }

  [SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by Dapper")]
  private sealed class DiscoveryResult
  {
    [Column("server")] public string Server { get; set; } = string.Empty;

    [Column("connection_string")] public string ConnectionString { get; set; } = string.Empty;
  }
}