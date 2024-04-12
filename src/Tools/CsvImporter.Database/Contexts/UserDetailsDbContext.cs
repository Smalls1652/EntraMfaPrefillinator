using EntraMfaPrefillinator.Lib.Models;

using Microsoft.EntityFrameworkCore;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Database.Contexts;

public class UserDetailsDbContext : DbContext
{
    public UserDetailsDbContext(DbContextOptions<UserDetailsDbContext> options) : base(options)
    {}

    public DbSet<UserDetails> UserDetails { get; set; } = null!;
}
