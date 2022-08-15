using CrossLaunch.Models;
using CrossLaunch.Tests.Utility;
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
    public async Task AddProjectDirectoryAsync_New_Adds()
    {
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(0));
        var projectDirectory = CreateProjectDirectory("test");
        await _db.AddProjectDirectoryAsync(projectDirectory);
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task AddProjectDirectoryAsync_Existing_Adds()
    {
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(0));
        var projectDirectory = CreateProjectDirectory("test");
        await _db.AddProjectDirectoryAsync(projectDirectory);
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(1));
        var projectDirectory2 = CreateProjectDirectory("test");
        await _db.AddProjectDirectoryAsync(projectDirectory2);
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task RemoveProjectDirectoryAsync_Existing_Removed()
    {
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(0));
        var projectDirectory = CreateProjectDirectory("test");
        _db.ProjectDirectories.Add(projectDirectory);
        await _db.SaveChangesAsync();
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(1));
        await _db.RemoveProjectDirectoryAsync(projectDirectory);
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task RemoveProjectDirectoryAsync_ExistingAltObject_Removed()
    {
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(0));
        var projectDirectory = CreateProjectDirectory("test");
        _db.ProjectDirectories.Add(projectDirectory);
        await _db.SaveChangesAsync();
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(1));
        await _db.RemoveProjectDirectoryAsync(CreateProjectDirectory("test"));
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task RemoveProjectDirectoryAsync_UntrackedAndNonexistent_Noop()
    {
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(0));
        var projectDirectory = CreateProjectDirectory("test");
        _db.ProjectDirectories.Add(projectDirectory);
        await _db.SaveChangesAsync();
        Assert.That(await _db.ProjectDirectories.CountAsync(), Is.EqualTo(1));
        await _db.RemoveProjectDirectoryAsync(CreateProjectDirectory("test2"));
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
        for (int i = 0; i < 3; i++)
        {
            var recent = recents[0];
            recents.RemoveAt(0);
            recents.Add(recent);
            await _db.PushRecentProjectAsync(_config, recent);
            Assert.That(await _db.RecentProjects.ToListAsync(), Is.EquivalentTo(recents.TakeLast(3)));
            await Task.Delay(TimeSpan.FromMilliseconds(5));
        }
    }

    [Test]
    public async Task UpdateProjectDirectoryProjectListAsync_Initial_Creates()
    {
        var dir = CreateProjectDirectory("x1");
        await _db.AddProjectDirectoryAsync(dir);
        var evaluator = new TestEvaluator(new RelativeEvaluatedProject("x1/p0", "v1"), new RelativeEvaluatedProject("x1/p1", "v1"));
        await _db.UpdateProjectDirectoryProjectListAsync(dir, new[] { evaluator });
        Assert.Multiple(async () =>
        {
            Assert.That(await _db.ProjectDirectoryProjects.FindAsync(Path.GetFullPath("x1/p0")), Is.Not.Null);
            Assert.That(await _db.ProjectDirectoryProjects.FindAsync(Path.GetFullPath("x1/p1")), Is.Not.Null);
        });
    }

    [Test]
    public async Task UpdateProjectDirectoryProjectListAsync_UpdateWithAdded_Adds()
    {
        var dir = CreateProjectDirectory("x1");
        await _db.AddProjectDirectoryAsync(dir);
        _db.ProjectDirectoryProjects.Add(CreateProjectDirectoryProject("x1/p0", dir));
        _db.ProjectDirectoryProjects.Add(CreateProjectDirectoryProject("x1/p1", dir));
        await _db.SaveChangesAsync();
        var evaluator = new TestEvaluator(new RelativeEvaluatedProject("x1/p0", "v1"), new RelativeEvaluatedProject("x1/p1", "v1"), new RelativeEvaluatedProject("x1/p2", "v1"));
        await _db.UpdateProjectDirectoryProjectListAsync(dir, new[] { evaluator });
        Assert.Multiple(async () =>
        {
            Assert.That(await _db.ProjectDirectoryProjects.FindAsync(Path.GetFullPath("x1/p0")), Is.Not.Null);
            Assert.That(await _db.ProjectDirectoryProjects.FindAsync(Path.GetFullPath("x1/p1")), Is.Not.Null);
            Assert.That(await _db.ProjectDirectoryProjects.FindAsync(Path.GetFullPath("x1/p2")), Is.Not.Null);
        });
    }

    [Test]
    public async Task UpdateProjectDirectoryProjectListAsync_UpdateWithUpdated_Updates()
    {
        var dir = CreateProjectDirectory("x1");
        await _db.AddProjectDirectoryAsync(dir);
        _db.ProjectDirectoryProjects.Add(CreateProjectDirectoryProject("x1/p0", dir, framework: "v0"));
        await _db.SaveChangesAsync();
        Assert.That((await _db.ProjectDirectoryProjects.FindAsync(Path.GetFullPath("x1/p0")))?.Framework, Is.EqualTo("v0"));
        var evaluator = new TestEvaluator(new RelativeEvaluatedProject("x1/p0", "v2"));
        await _db.UpdateProjectDirectoryProjectListAsync(dir, new[] { evaluator });
        Assert.That((await _db.ProjectDirectoryProjects.FindAsync(Path.GetFullPath("x1/p0")))?.Framework, Is.EqualTo("v2"));
    }

    [Test]
    public async Task UpdateProjectDirectoryProjectListAsync_UpdateWithRemoved_Deletes()
    {
        var dir = CreateProjectDirectory("x1");
        await _db.AddProjectDirectoryAsync(dir);
        _db.ProjectDirectoryProjects.Add(CreateProjectDirectoryProject("x1/p0", dir));
        _db.ProjectDirectoryProjects.Add(CreateProjectDirectoryProject("x1/p1", dir));
        await _db.SaveChangesAsync();
        var evaluator = new TestEvaluator(new RelativeEvaluatedProject("x1/p0", "v1"));
        await _db.UpdateProjectDirectoryProjectListAsync(dir, new[] { evaluator });
        Assert.Multiple(async () =>
        {
            Assert.That(await _db.ProjectDirectoryProjects.FindAsync(Path.GetFullPath("x1/p0")), Is.Not.Null);
            Assert.That(await _db.ProjectDirectoryProjects.FindAsync(Path.GetFullPath("x1/p1")), Is.Null);
        });
    }

    private static RecentProjectModel CreateRecent(string path, string projectEvaluatorType = "lol", string framework = "xd")
    {
        var now = DateTime.Now;
        return new RecentProjectModel
        {
            FullPath = Path.GetFullPath(path),
            Framework = framework,
            RecordUpdateTime = now,
            OpenedTime = now,
            ProjectEvaluatorType = projectEvaluatorType
        };
    }

    private static ProjectDirectoryModel CreateProjectDirectory(string path)
    {
        return new ProjectDirectoryModel { FullPath = Path.GetFullPath(path), Projects = new HashSet<ProjectDirectoryProjectModel>(), RecordUpdateTime = DateTime.Now };
    }

    private static ProjectDirectoryProjectModel CreateProjectDirectoryProject(string path, ProjectDirectoryModel projectDirectory, string projectEvaluatorType = "lol", string framework = "xd")
    {
        var now = DateTime.Now;
        return new ProjectDirectoryProjectModel
        {
            FullPath = Path.GetFullPath(path),
            Framework = framework,
            RecordUpdateTime = now,
            ProjectEvaluatorType = projectEvaluatorType,
            ProjectDirectory = projectDirectory
        };
    }

    public record RelativeEvaluatedProject(string Path, string Framework);

    public class TestEvaluator : IProjectEvaluator
    {
        private readonly Dictionary<string, EvaluatedProject> _evaluatedProjects;

        public TestEvaluator(params RelativeEvaluatedProject[] relativeEvaluatedProjects)
        {
            _evaluatedProjects = relativeEvaluatedProjects.Select(v => new EvaluatedProject(Path.GetFullPath(v.Path), v.Framework)).ToDictionary(v => v.FullPath);
        }

        public TestEvaluator(params EvaluatedProject[] evaluatedProjects)
        {
            _evaluatedProjects = evaluatedProjects.ToDictionary(v => v.FullPath);
        }

        public Task<EvaluatedProject?> EvaluateProjectAsync(string path, CancellationToken cancellationToken = default)
        {
            string fullPath = Path.GetFullPath(path);
            return Task.FromResult(_evaluatedProjects.TryGetValue(fullPath, out var evaluatedProject) ? evaluatedProject : null);
        }

        public IAsyncEnumerable<EvaluatedProject> FindProjectsAsync(string path, CancellationToken cancellationToken = default)
        {
            string fullPath = Path.GetFullPath(path);
            return _evaluatedProjects.Values.Where(v => PathContains(fullPath, v.FullPath)).AsAsyncEnumerable();
        }
    }

    private static bool PathContains(string root, string sub)
    {
        ReadOnlySpan<char> rootS = root;
        ReadOnlySpan<char> subS = sub;
        if (Path.EndsInDirectorySeparator(rootS)) rootS = rootS[..^1];
        if (Path.EndsInDirectorySeparator(subS)) subS = subS[..^1];
        if (rootS.Length > subS.Length) return false;
        if (!rootS.SequenceEqual(subS[..rootS.Length])) return false;
        if (rootS.Length == subS.Length) return true;
        char subS0 = subS[0];
        return subS0 == Path.DirectorySeparatorChar || subS0 == Path.AltDirectorySeparatorChar;
    }
}
