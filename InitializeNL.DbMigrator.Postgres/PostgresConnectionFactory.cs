using System.Data;
using InitializeNL.DbMigrator;
using Npgsql;

namespace InitializeNL.DbMigrator.Postgres;

/// <summary>
/// Creates Npgsql connections from a connection string.
/// </summary>
public class PostgresConnectionFactory : IDbConnectionFactory
{
  private readonly string _connectionString;
  private readonly string _description;

  public PostgresConnectionFactory(string connectionString)
  {
    _connectionString = connectionString;
    NpgsqlConnectionStringBuilder builder = new(connectionString);
    _description = $"{builder.Host}/{builder.Database}";
  }

  public PostgresConnectionFactory(string connectionString, string description)
  {
    _connectionString = connectionString;
    _description = description;
  }

  public IDbConnection CreateConnection()
  {
    return new NpgsqlConnection(_connectionString);
  }

  public string ConnectionDescription => _description;
}