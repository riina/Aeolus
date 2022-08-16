using CrossLaunch;
using Microsoft.EntityFrameworkCore;

namespace cl;

public class CLContext : CLContextBase
{
    public CLContext(DbContextOptions options) : base(options)
    {
    }
}
