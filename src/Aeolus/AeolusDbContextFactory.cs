using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aeolus;

public class AeolusDbContextFactory : IDesignTimeDbContextFactory<AeolusDbContext>
{
    public virtual Assembly MigrationAssembly => GetType().Assembly;

    public AeolusDbContext CreateDbContext(string[] args)
    {
        var ob = new DbContextOptionsBuilder<AeolusDbContext>();
        ob.UseSqlite($"Data Source=tmp_aeolus.db;",
            b => b.MigrationsAssembly(MigrationAssembly.FullName));
        return new AeolusDbContext(ob.Options);
    }
}
