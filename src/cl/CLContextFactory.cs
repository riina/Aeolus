using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace cl;

public class CLContextFactory : IDesignTimeDbContextFactory<CLContext>
{
    public virtual Assembly MigrationAssembly => GetType().Assembly;

    public CLContext CreateDbContext(string[] args)
    {
        var ob = new DbContextOptionsBuilder<CLContext>();
        ob.UseSqlite("Data Source=cl.db;",
            b => b.MigrationsAssembly(MigrationAssembly.FullName));
        ob.UseLazyLoadingProxies();
        return new CLContext(ob.Options);
    }
}
