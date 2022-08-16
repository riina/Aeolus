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
        Directory.CreateDirectory(CLFiles.DataDirectory);
        ob.UseSqlite(@$"Data Source=""{Path.Combine(CLFiles.DataDirectory, "cl.db")}"";",
            b => b.MigrationsAssembly(MigrationAssembly.FullName));
        ob.UseLazyLoadingProxies();
        return new CLContext(ob.Options);
    }
}
