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
        string dir;
        if (OperatingSystem.IsMacOS())
        {
            // .config
            dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify), "CrossLaunchCL");
        }
        else if (OperatingSystem.IsWindows())
        {
            // AppData/Roaming
            dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify), "CrossLaunchCL");
        }
        else
        {
            dir = Environment.CurrentDirectory;
        }
        Directory.CreateDirectory(dir);
        ob.UseSqlite(@$"Data Source=""{Path.Combine(dir, "cl.db")}"";",
            b => b.MigrationsAssembly(MigrationAssembly.FullName));
        ob.UseLazyLoadingProxies();
        return new CLContext(ob.Options);
    }
}
