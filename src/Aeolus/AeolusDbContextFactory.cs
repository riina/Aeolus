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
        Directory.CreateDirectory(AeolusFiles.DataDirectory);
        ob.UseSqlite(@$"Data Source=""{Path.Combine(AeolusFiles.DataDirectory, "cl.db")}"";",
            b => b.MigrationsAssembly(MigrationAssembly.FullName));
        ob.UseLazyLoadingProxies();
        return new AeolusDbContext(ob.Options);
    }
}
