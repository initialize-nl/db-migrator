using InitializeNL.DbMigrator;
using InitializeNL.DbMigrator.Postgres;
using InitializeNL.DbMigrator.Sources;
using Microsoft.Extensions.Logging;

// Example: Single database migration
async Task SingleDatabaseExample()
{
  Console.WriteLine("=== Single Database Migration ===\n");

  string connectionString = "Host=localhost;Database=example_db;Username=postgres;Password=postgres";

  using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
                                                              builder.AddConsole()
                                                                .SetMinimumLevel(LogLevel.Information));

  MigrationRunner runner = new MigratorBuilder()
    .UseLoggerFactory(loggerFactory)
    .UseMigrationSource(new FileSystemMigrationSource("./migrations"))
    .UseConnectionFactory(new PostgresConnectionFactory(connectionString))
    .UseMigrationLock(new PostgresAdvisoryLock())
    .UseMigrationTracker(new PostgresMigrationTracker())
    .Build();

  MigrationResult result = await runner.MigrateAsync();

  Console.WriteLine($"\nResult: {result.Message}");
  Console.WriteLine($"Executed: {string.Join(", ", result.ExecutedMigrations)}");
}

// Example: Parallel multi-database migration
async Task ParallelMigrationExample()
{
  Console.WriteLine("=== Parallel Multi-Database Migration ===\n");

  string[] connectionStrings =
  [
    "Host=localhost;Database=tenant_1;Username=postgres;Password=postgres",
    "Host=localhost;Database=tenant_2;Username=postgres;Password=postgres",
    "Host=localhost;Database=tenant_3;Username=postgres;Password=postgres",
  ];

  using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
                                                              builder.AddConsole()
                                                                .SetMinimumLevel(LogLevel.Information));

  ParallelMigrationRunner runner = new ParallelMigratorBuilder()
    .UseLoggerFactory(loggerFactory)
    .UseMigrationSource(new FileSystemMigrationSource("./migrations"))
    .UseMigrationLock(new PostgresAdvisoryLock())
    .UseMigrationTracker(new PostgresMigrationTracker())
    .WithParallelism(3)
    .Build();

  IEnumerable<IDbConnectionFactory> factories = connectionStrings
    .Select(cs => new PostgresConnectionFactory(cs));

  ParallelMigrationResult result = await runner.MigrateAsync(factories);

  Console.WriteLine($"\nAll succeeded: {result.AllSucceeded}");
  foreach (DatabaseMigrationResult dbResult in result.Results)
  {
    Console.WriteLine($"  {dbResult.ConnectionDescription}: {dbResult.Message}");
    if (dbResult.ExecutedMigrations.Count > 0)
    {
      Console.WriteLine($"    Executed: {string.Join(", ", dbResult.ExecutedMigrations)}");
    }
  }
}

// Example: Using embedded resources (for compiled migrations)
async Task EmbeddedResourceExample()
{
  Console.WriteLine("=== Embedded Resource Migration ===\n");

  string connectionString = "Host=localhost;Database=example_db;Username=postgres;Password=postgres";

  using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
                                                              builder.AddConsole()
                                                                .SetMinimumLevel(LogLevel.Information));

  // Load migrations from embedded resources in an assembly
  // The assembly should have .sql files as embedded resources
  MigrationRunner runner = new MigratorBuilder()
    .UseLoggerFactory(loggerFactory)
    .UseMigrationSource(
      new EmbeddedResourceMigrationSource(
        typeof(Program).Assembly,
        "InitializeNL.DbMigrator.Example.migrations"))
    .UseConnectionFactory(new PostgresConnectionFactory(connectionString))
    .UseMigrationLock(new PostgresAdvisoryLock())
    .UseMigrationTracker(new PostgresMigrationTracker())
    .Build();

  MigrationResult result = await runner.MigrateAsync();

  Console.WriteLine($"\nResult: {result.Message}");
}

// Run the example
string mode = args.Length > 0 ? args[0] : "single";

switch (mode)
{
  case "single":
    await SingleDatabaseExample();
    break;
  case "parallel":
    await ParallelMigrationExample();
    break;
  case "embedded":
    await EmbeddedResourceExample();
    break;
  default:
    Console.WriteLine("Usage: dotnet run -- [single|parallel|embedded]");
    break;
}