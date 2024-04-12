using EntraMfaPrefillinator.Lib.Models;

using Microsoft.EntityFrameworkCore;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Database.Contexts;

/// <summary>
/// Database context for <see cref="UserDetails"/>.
/// </summary>
public class UserDetailsDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserDetailsDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public UserDetailsDbContext(DbContextOptions<UserDetailsDbContext> options) : base(options)
    {}

    /// <summary>
    /// Items in the 'UserDetails' table.
    /// </summary>
    public DbSet<UserDetails> UserDetails { get; set; } = null!;
}
