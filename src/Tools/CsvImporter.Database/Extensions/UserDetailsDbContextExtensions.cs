using System.Data;

using EntraMfaPrefillinator.Tools.CsvImporter.Database.Contexts;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Database.Extensions;

/// <summary>
/// Extension methods for <see cref="UserDetailsDbContext"/>.
/// </summary>
public static class UserDetailsDbContextExtensions
{
    /// <summary>
    /// Adds a <see cref="UserDetailsDbContext"/> to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="dbPath">The path to the SQLite database file.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddUserDetailsDbContext(this IServiceCollection services, string dbPath)
    {
        services.AddDbContext<UserDetailsDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dbPath}");
        });

        return services;
    }

    /// <summary>
    /// Adds a <see cref="UserDetailsDbContext"/> factory to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="dbPath">The path to the SQLite database file.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddUserDetailsDbContextFactory(this IServiceCollection services, string dbPath)
    {
        services.AddDbContextFactory<UserDetailsDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dbPath}");
        });

        return services;
    }

    /// <summary>
    /// Applies migrations to the <see cref="UserDetailsDbContext"/>.
    /// </summary>
    /// <remarks>
    /// This method only works when <see cref="AddUserDetailsDbContext(IServiceCollection, string)"/> is used.
    /// </remarks>
    /// <param name="host">The app host.</param>
    /// <returns></returns>
    public static async Task ApplyUserDetailsDbContextMigrations(this IHost host)
    {
        using IServiceScope scope = host.Services.CreateScope();

        using UserDetailsDbContext dbContext = scope.ServiceProvider.GetRequiredService<UserDetailsDbContext>();

        bool isPreEfCoreMigration = await dbContext.ApplyPreEfCoreMigrations_RenameUserDetailsTableAsync();

        await dbContext.Database.MigrateAsync();

        if (isPreEfCoreMigration)
        {
            await dbContext.ApplyPreEfCoreMigrations_TransferOldUserDetailsTableAsync();
        }
    }

    /// <summary>
    /// Applies migrations to the <see cref="UserDetailsDbContext"/> factory.
    /// </summary>
    /// <remarks>
    /// This method only works when <see cref="AddUserDetailsDbContextFactory(IServiceCollection, string)"/> is used.
    /// </remarks>
    /// <param name="host">The app host.</param>
    /// <returns></returns>
    public static async Task ApplyUserDetailsDbContextFactoryMigrations(this IHost host)
    {
        using IServiceScope scope = host.Services.CreateScope();

        IDbContextFactory<UserDetailsDbContext> dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<UserDetailsDbContext>>();

        bool isPreEfCoreMigration = await dbContextFactory.ApplyPreEfCoreMigrations_RenameUserDetailsTableAsync();

        using UserDetailsDbContext dbContext = dbContextFactory.CreateDbContext();

        await dbContext.Database.MigrateAsync();

        if (isPreEfCoreMigration)
        {
            await dbContextFactory.ApplyPreEfCoreMigrations_TransferOldUserDetailsTableAsync();
        }
    }

    /// <summary>
    /// Get if the SQLite database when the database is in a pre-EF Core state
    /// and apply the first step of the pre-EF Core migration.
    /// </summary>
    /// <param name="dbContextFactory">The <see cref="UserDetailsDbContext"/> factory.</param>
    /// <returns>Whether the database is in a pre-EF Core state.</returns>
    private static async Task<bool> ApplyPreEfCoreMigrations_RenameUserDetailsTableAsync(this IDbContextFactory<UserDetailsDbContext> dbContextFactory)
    {
        using UserDetailsDbContext dbContext = dbContextFactory.CreateDbContext();

        return await dbContext.ApplyPreEfCoreMigrations_RenameUserDetailsTableAsync();
    }

    /// <summary>
    /// Get if the SQLite database when the database is in a pre-EF Core state
    /// and apply the first step of the pre-EF Core migration.
    /// </summary>
    /// <param name="dbContext">The <see cref="UserDetailsDbContext"/>.</param>
    /// <returns>Whether the database is in a pre-EF Core state.</returns>
    private static async Task<bool> ApplyPreEfCoreMigrations_RenameUserDetailsTableAsync(this UserDetailsDbContext dbContext)
    {
        var dbBuilder = dbContext.Database.GetService<IRelationalDatabaseCreator>();

        if (!dbBuilder.Exists())
        {
            return false;
        }
        else
        {
            var migrationsTable = dbContext.Database
                .SqlQuery<string>($"SELECT name FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'")
                .AsEnumerable()
                .FirstOrDefault();

            var userDetailsTable = dbContext.Database
                .SqlQuery<string>($"SELECT name FROM sqlite_master WHERE type='table' AND name='UserDetails'")
                .AsEnumerable()
                .SingleOrDefault();

            if (migrationsTable is not null && userDetailsTable is not null || migrationsTable is null && userDetailsTable is null)
            {
                return false;
            }
            else
            {
                using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddSimpleConsole(options =>
                    {
                        options.SingleLine = false;
                        options.UseUtcTimestamp = true;
                    });
                });

                ILogger logger = loggerFactory.CreateLogger("PreEfCoreMigrations");

                logger.LogWarning("The database is in a pre-EF Core state. Performing pre-EF Core migration...");

                logger.LogWarning("Renaming 'UserDetails' table to 'UserDetails_old'...");

                await dbContext.Database
                    .BeginTransactionAsync();

                await dbContext.Database
                    .ExecuteSqlRawAsync($"ALTER TABLE UserDetails RENAME TO UserDetails_old;");

                await dbContext.Database
                    .CommitTransactionAsync();

                return true;
            }
        }
    }

    /// <summary>
    /// Apply the second step of the pre-EF Core migration by transferring data from 'UserDetails_old' to 'UserDetails'
    /// and dropping the 'UserDetails_old' table once complete.
    /// </summary>
    /// <param name="dbContextFactory>">The <see cref="UserDetailsDbContext"/> factory.</param>
    /// <returns></returns>
    private static async Task ApplyPreEfCoreMigrations_TransferOldUserDetailsTableAsync(this IDbContextFactory<UserDetailsDbContext> dbContextFactory)
    {
        using UserDetailsDbContext dbContext = dbContextFactory.CreateDbContext();

        await dbContext.ApplyPreEfCoreMigrations_TransferOldUserDetailsTableAsync();
    }

    /// <summary>
    /// Apply the second step of the pre-EF Core migration by transferring data from 'UserDetails_old' to 'UserDetails'
    /// and dropping the 'UserDetails_old' table once complete.
    /// </summary>
    /// <param name="dbContext">The <see cref="UserDetailsDbContext"/>.</param>
    /// <returns></returns>
    private static async Task ApplyPreEfCoreMigrations_TransferOldUserDetailsTableAsync(this UserDetailsDbContext dbContext)
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = false;
                options.UseUtcTimestamp = true;
            });
        });

        ILogger logger = loggerFactory.CreateLogger("PreEfCoreMigrations");

        logger.LogWarning("Transferring data from 'UserDetails_old' to 'UserDetails'...");

        await dbContext.Database
            .BeginTransactionAsync();

        await dbContext.Database
            .ExecuteSqlRawAsync($"""
INSERT INTO UserDetails SELECT * FROM UserDetails_old;

DROP TABLE UserDetails_old;
""");

        await dbContext.Database
            .CommitTransactionAsync();

        logger.LogInformation("Pre-EF Core migration complete.");
    }
}