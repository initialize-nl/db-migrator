using System.Data;

namespace InitializeNL.DbMigrator;

/// <summary>
/// Factory for creating database connections.
/// </summary>
public interface IDbConnectionFactory
{
  /// <summary>
  /// Creates a new database connection.
  /// </summary>
  IDbConnection CreateConnection();

  /// <summary>
  /// A human-readable description of the connection (e.g., "server/database").
  /// Used for logging and result reporting.
  /// </summary>
  string ConnectionDescription { get; }
}