using Microsoft.EntityFrameworkCore;

namespace CrossLaunch.Tests;

public class TestDbContext : CLContextBase
{
    public TestDbContext(DbContextOptions options) : base(options)
    {
    }
}
