using CrossLaunch;
using Microsoft.EntityFrameworkCore;

namespace cl;

public static class CLInstanceEX
{
    public static async Task<CLInstance> CreateAsync(CLConfiguration configuration)
    {
        var dbFac = new CLContextFactory();
        return await CLInstance.CreateAsync(configuration, dbFac.CreateDbContext(Array.Empty<string>()));
    }
}
