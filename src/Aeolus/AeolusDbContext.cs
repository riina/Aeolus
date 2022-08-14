using CrossLaunch;
using Microsoft.EntityFrameworkCore;

namespace Aeolus;

public class AeolusDbContext : CLContextBase
{
    public AeolusDbContext(DbContextOptions options) : base(options)
    {
    }
}
