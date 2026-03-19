namespace InitializeNL.DbMigrator;

/// <summary>
/// Simple command-line interface for running migrations.
/// Allows embedding migration commands into host applications.
/// </summary>
public static class MigrationCli
{
  /// <summary>
  /// Runs a migration command based on the provided arguments.
  /// </summary>
  /// <param name="args">Command arguments (e.g., ["migrate"], ["status"], ["rollback", "--target=001"])</param>
  /// <param name="runner">The configured migration runner</param>
  /// <param name="output">Optional output writer (defaults to Console.Out)</param>
  /// <returns>Exit code: 0 for success, non-zero for failure</returns>
  public static async Task<int> RunAsync(string[] args, MigrationRunner runner, TextWriter? output = null)
  {
    output ??= Console.Out;

    if (args.Length == 0)
    {
      PrintUsage(output);
      return 1;
    }

    string command = args[0].ToLowerInvariant();
    Dictionary<string, string?> options = ParseOptions(args.Length > 1 ? args[1..] : []);

    return command switch
    {
      "migrate" => await MigrateAsync(runner, options, output).ConfigureAwait(false),
      "status" => await StatusAsync(runner, output).ConfigureAwait(false),
      "rollback" => await RollbackAsync(runner, options, output).ConfigureAwait(false),
      "help" or "--help" or "-h" => PrintUsage(output),
      _ => UnknownCommand(command, output),
    };
  }

  private static async Task<int> MigrateAsync(
    MigrationRunner runner,
    Dictionary<string, string?> options,
    TextWriter output)
  {
    string? target = options.GetValueOrDefault("target");
    bool fillGaps = options.ContainsKey("fill-gaps");

    MigrationResult result = await runner.MigrateAsync(target, fillGaps).ConfigureAwait(false);

    await output.WriteLineAsync(result.Message).ConfigureAwait(false);
    foreach (string migration in result.ExecutedMigrations)
    {
      await output.WriteLineAsync($"  {migration}").ConfigureAwait(false);
    }

    return result.Success ? 0 : 1;
  }

  private static async Task<int> StatusAsync(MigrationRunner runner, TextWriter output)
  {
    MigrationStatus status = await runner.GetStatusAsync().ConfigureAwait(false);

    await output.WriteLineAsync("Applied migrations:").ConfigureAwait(false);
    if (status.Applied.Count == 0)
    {
      await output.WriteLineAsync("  (none)").ConfigureAwait(false);
    }
    else
    {
      foreach (string m in status.Applied)
      {
        await output.WriteLineAsync($"  [x] {m}").ConfigureAwait(false);
      }
    }

    await output.WriteLineAsync().ConfigureAwait(false);
    await output.WriteLineAsync("Pending migrations:").ConfigureAwait(false);
    if (status.Pending.Count == 0)
    {
      await output.WriteLineAsync("  (none)").ConfigureAwait(false);
    }
    else
    {
      foreach (string m in status.Pending)
      {
        await output.WriteLineAsync($"  [ ] {m}").ConfigureAwait(false);
      }
    }

    return 0;
  }

  private static async Task<int> RollbackAsync(
    MigrationRunner runner,
    Dictionary<string, string?> options,
    TextWriter output)
  {
    string? target = options.GetValueOrDefault("target");

    if (target == null)
    {
      MigrationStatus status = await runner.GetStatusAsync().ConfigureAwait(false);
      if (status.Applied.Count == 0)
      {
        await output.WriteLineAsync("No migrations applied. Nothing to rollback.").ConfigureAwait(false);
        return 1;
      }

      if (status.Applied.Count == 1)
      {
        await output
          .WriteLineAsync("Only one migration applied. Use --target to specify rollback target or rollback manually.")
          .ConfigureAwait(false);
        return 1;
      }

      // Rollback one step: target is the second-to-last applied migration
      target = status.Applied[^2];
    }

    MigrationResult result = await runner.MigrateAsync(target).ConfigureAwait(false);

    await output.WriteLineAsync(result.Message).ConfigureAwait(false);
    foreach (string migration in result.ExecutedMigrations)
    {
      await output.WriteLineAsync($"  {migration}").ConfigureAwait(false);
    }

    return result.Success ? 0 : 1;
  }

  private static Dictionary<string, string?> ParseOptions(string[] args)
  {
    Dictionary<string, string?> options = [];
    foreach (string arg in args)
    {
      if (arg.StartsWith("--"))
      {
        int equalsIndex = arg.IndexOf('=');
        if (equalsIndex > 0)
        {
          string key = arg[2..equalsIndex];
          string value = arg[(equalsIndex + 1)..];
          options[key] = value;
        }
        else
        {
          options[arg[2..]] = null;
        }
      }
    }

    return options;
  }

  private static int PrintUsage(TextWriter output)
  {
    output.WriteLine("Database Migration CLI");
    output.WriteLine();
    output.WriteLine("Commands:");
    output.WriteLine("  migrate              Run pending migrations");
    output.WriteLine("  status               Show migration status");
    output.WriteLine("  rollback             Rollback the last migration");
    output.WriteLine("  help                 Show this help");
    output.WriteLine();
    output.WriteLine("Options:");
    output.WriteLine("  --target=<name>      Migrate/rollback to specific migration");
    output.WriteLine("  --fill-gaps          Allow filling gaps in migration history");
    output.WriteLine();
    output.WriteLine("Examples:");
    output.WriteLine("  myapp db migrate");
    output.WriteLine("  myapp db migrate --target=002_AddUsers");
    output.WriteLine("  myapp db status");
    output.WriteLine("  myapp db rollback");
    return 0;
  }

  private static int UnknownCommand(string command, TextWriter output)
  {
    output.WriteLine($"Unknown command: {command}");
    output.WriteLine("Use 'help' for available commands.");
    return 1;
  }
}