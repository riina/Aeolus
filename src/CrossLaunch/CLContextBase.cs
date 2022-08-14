using CrossLaunch.Models;
using Microsoft.EntityFrameworkCore;

namespace CrossLaunch;

public class CLContextBase : DbContext
{
    public DbSet<RecentProjectModel> RecentProjects { get; set; } = null!;

    public DbSet<ProjectDirectoryProjectModel> ProjectDirectoryProjects { get; set; } = null!;

    public DbSet<ProjectDirectoryModel> ProjectDirectories { get; set; } = null!;

    public CLContextBase(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CLContextBase).Assembly);
    }
}
