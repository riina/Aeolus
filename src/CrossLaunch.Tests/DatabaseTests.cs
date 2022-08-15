using CrossLaunch.Models;
using Microsoft.EntityFrameworkCore;

namespace CrossLaunch.Tests;

public class DatabaseTests
{
    private TestDbContext _db = null!;
    private CLConfiguration _config = null!;

    [SetUp]
    public void Setup()
    {
        var ob = new DbContextOptionsBuilder<TestDbContext>();
        ob.UseSqlite("DataSource=file::memory:?cache=shared;");
        _db = new TestDbContext(ob.Options);
        _db.Database.EnsureCreated();
        _db.RecentProjects.RemoveRange(_db.RecentProjects);
        _db.ProjectDirectoryProjects.RemoveRange(_db.ProjectDirectoryProjects);
        _db.ProjectDirectories.RemoveRange(_db.ProjectDirectories);
        _db.SaveChanges();
        _config = new CLConfiguration();
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    [Test]
    public async Task AddProjectDirectoryAsync_Adds()
    {
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(0));
        var projectDirectory = CreateDirectory("test");
        await _db.AddProjectDirectoryAsync(projectDirectory);
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task RemoveProjectDirectoryAsync_Existing_Removed()
    {
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(0));
        var projectDirectory = CreateDirectory("test");
        _db.ProjectDirectories.Add(projectDirectory);
        await _db.SaveChangesAsync();
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(1));
        await _db.RemoveProjectDirectoryAsync(projectDirectory);
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task RemoveProjectDirectoryAsync_Untracked_Noop()
    {
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(0));
        var projectDirectory = CreateDirectory("test");
        _db.ProjectDirectories.Add(projectDirectory);
        await _db.SaveChangesAsync();
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(1));
        Assert.That(async () => await _db.RemoveProjectDirectoryAsync(CreateDirectory("test")), Throws.InstanceOf<InvalidOperationException>());
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task RemoveProjectDirectoryAsync_UntrackedAndNonexistent_Noop()
    {
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(0));
        var projectDirectory = CreateDirectory("test");
        _db.ProjectDirectories.Add(projectDirectory);
        await _db.SaveChangesAsync();
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(1));
        Assert.That(async () => await _db.RemoveProjectDirectoryAsync(CreateDirectory("test2")), Throws.InstanceOf<DbUpdateConcurrencyException>());
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task PushRecentProjectAsync_Cycles()
    {
        _config.MaxRecentProjects = 3;
        List<RecentProjectModel> recents = new();
        for (int i = 0; i < 3; i++)
        {
            var recent = CreateRecent($"{i}");
            recents.Add(recent);
            await _db.PushRecentProjectAsync(_config, recent);
            await Task.Delay(TimeSpan.FromMilliseconds(5));
        }
        Assert.That(await _db.RecentProjects.ToListAsync(), Is.EquivalentTo(recents));
        for (int i = 0; i < 3; i++)
        {
            var recent = CreateRecent($"{i + 5}");
            recents.Add(recent);
            await _db.PushRecentProjectAsync(_config, recent);
            Assert.That(await _db.RecentProjects.ToListAsync(), Is.EquivalentTo(recents.TakeLast(3)));
            await Task.Delay(TimeSpan.FromMilliseconds(5));
        }
    }

    private static RecentProjectModel CreateRecent(string fullPath, string projectEvaluatorType = "lol", string framework = "xd")
    {
        var now = DateTime.Now;
        return new RecentProjectModel
        {
            FullPath = fullPath,
            Framework = framework,
            RecordUpdateTime = now,
            OpenedTime = now,
            ProjectEvaluatorType = projectEvaluatorType
        };
    }

    private static ProjectDirectoryModel CreateDirectory(string fullPath)
    {
        return new ProjectDirectoryModel { FullPath = fullPath, Projects = new HashSet<ProjectDirectoryProjectModel>(), RecordUpdateTime = DateTime.Now };
    }
}
