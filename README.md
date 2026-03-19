# InitializeNL.DbMigrator

A lightweight PostgreSQL database migration library for .NET 10+.

## Features

- Up/down SQL migrations with timestamp-based ordering
- Code migrations via `[Migration]` attribute
- Advisory lock or table-based distributed locking
- Parallel multi-server, multi-database migration
- Migration history audit log with duration and hostname tracking
- Explicit destructive script flagging (`-- arg: destructive`)
- Configurable table names
- Pluggable architecture (migration sources, trackers, locks, connection providers)
- Environment variable support (`DBMIGRATOR_CONNECTION`, `DBMIGRATOR_PASSWORD`)

## Packages

| Package | Description |
|---------|-------------|
| `InitializeNL.DbMigrator` | Core library with interfaces and runners |
| `InitializeNL.DbMigrator.Postgres` | PostgreSQL provider (tracker, locking, connection providers) |

## Quick Start

### As a library

```csharp
var runner = new MigratorBuilder()
    .UseConnectionFactory(new PostgresConnectionFactory("Host=localhost;Database=mydb"))
    .UseMigrationTracker(new PostgresMigrationTracker())
    .UseMigrationLock(new PostgresAdvisoryLock())
    .UseFileSystemSource("migrations")
    .Build();

await runner.MigrateAsync();
```

### As a CLI

```bash
dotnet run --project InitializeNL.DbMigrator.Cli -- \
    --connection-string "Host=localhost;Database=mydb;Username=postgres;Password=secret" \
    --source ./migrations \
    --execute
```

Or with environment variables:

```bash
export DBMIGRATOR_CONNECTION="Host=localhost;Database=mydb;Username=postgres"
export DBMIGRATOR_PASSWORD="secret"

dotnet run --project InitializeNL.DbMigrator.Cli -- \
    --source ./migrations \
    --execute
```

## Migration Scripts

Scripts follow the naming convention: `yyyy-MM-dd_HH-mm-ssZ_description.up.sql` / `.down.sql`

```
migrations/
  2024-01-01_00-00-00Z_create_users_table.up.sql
  2024-01-01_00-00-00Z_create_users_table.down.sql
  2024-01-02_00-00-00Z_add_posts_table.up.sql
  2024-01-02_00-00-00Z_add_posts_table.down.sql
```

### Script Arguments

```sql
-- arg: destructive
DROP TABLE old_data;
```

```sql
-- arg: irreversible
-- arg: repeat-until-affected-lte 0
DELETE FROM large_table WHERE expired < NOW() LIMIT 10000;
```

## CLI Options

| Option | Description |
|--------|-------------|
| `--connection-string` | Database connection string (or `DBMIGRATOR_CONNECTION` env var) |
| `--password` | Override password (or `DBMIGRATOR_PASSWORD` env var) |
| `--source` | Path to migration scripts directory |
| `--target` | Target migration name (latest when omitted) |
| `--execute` | Apply migrations (default is dry-run) |
| `--dry-run` | Preview mode, no changes applied |
| `--discovery-script` | SQL file for multi-tenant target discovery |
| `--yes` | Skip confirmation prompts |
| `--allow-destructive` | Skip destructive migration confirmation |
| `--no-lock` | Skip distributed locking |
| `--fill-gaps` | Execute missed migrations before the last applied one |
| `--server-parallelism` | Max servers to migrate in parallel (default: 1) |
| `--database-parallelism` | Max databases per server in parallel (default: 1) |

## License

[MIT](LICENSE)
