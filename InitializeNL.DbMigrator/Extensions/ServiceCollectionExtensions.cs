using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InitializeNL.DbMigrator.Extensions;

/// <summary>
/// Extension methods for registering the migration runner with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
  /// <summary>
  /// Adds a migration runner to the service collection.
  /// </summary>
  /// <param name="services">The service collection.</param>
  /// <param name="configure">Action to configure the migrator builder.</param>
  /// <returns>The service collection for chaining.</returns>
  public static IServiceCollection AddMigrator(
    this IServiceCollection services,
    Action<MigratorBuilder> configure)
  {
    services.AddSingleton(sp =>
    {
      MigratorBuilder builder = new();

      // Auto-wire ILoggerFactory from DI if available
      ILoggerFactory? loggerFactory = sp.GetService<ILoggerFactory>();
      if (loggerFactory != null)
      {
        builder.UseLoggerFactory(loggerFactory);
      }

      configure(builder);

      return builder.Build();
    });

    return services;
  }

  /// <summary>
  /// Adds a migration runner to the service collection with a builder factory.
  /// </summary>
  /// <param name="services">The service collection.</param>
  /// <param name="configure">Action to configure the migrator builder with access to service provider.</param>
  /// <returns>The service collection for chaining.</returns>
  public static IServiceCollection AddMigrator(
    this IServiceCollection services,
    Action<IServiceProvider, MigratorBuilder> configure)
  {
    services.AddSingleton(sp =>
    {
      MigratorBuilder builder = new();

      // Auto-wire ILoggerFactory from DI if available
      ILoggerFactory? loggerFactory = sp.GetService<ILoggerFactory>();
      if (loggerFactory != null)
      {
        builder.UseLoggerFactory(loggerFactory);
      }

      configure(sp, builder);

      return builder.Build();
    });

    return services;
  }
}