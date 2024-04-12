using System.Data;

using EntraMfaPrefillinator.Tools.CsvImporter.Database.Contexts;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Database.Extensions;

public static class UserDetailsDbContextExtensions
{
    public static IServiceCollection AddUserDetailsDbContext(this IServiceCollection services, string dbPath)
    {
        services.AddDbContext<UserDetailsDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dbPath}");
        });

        return services;
    }

    public static IServiceCollection AddUserDetailsDbContextFactory(this IServiceCollection services, string dbPath)
    {
        services.AddDbContextFactory<UserDetailsDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dbPath}");
        });

        return services;
    }

    public static async Task ApplyUserDetailsDbContextMigrations(this IHost host)
    {
        using IServiceScope scope = host.Services.CreateScope();

        using UserDetailsDbContext dbContext = scope.ServiceProvider.GetRequiredService<UserDetailsDbContext>();

        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.MigrateAsync();
        }
    }

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

    private static async Task<bool> ApplyPreEfCoreMigrations_RenameUserDetailsTableAsync(this IDbContextFactory<UserDetailsDbContext> dbContextFactory)
    {
        using UserDetailsDbContext dbContext = dbContextFactory.CreateDbContext();

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

    private static async Task ApplyPreEfCoreMigrations_TransferOldUserDetailsTableAsync(this IDbContextFactory<UserDetailsDbContext> dbContextFactory)
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

        using UserDetailsDbContext dbContext = dbContextFactory.CreateDbContext();

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