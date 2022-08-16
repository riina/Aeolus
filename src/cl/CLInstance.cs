using CrossLaunch;
using CrossLaunch.Models;
using Microsoft.EntityFrameworkCore;

namespace cl;

public sealed class CLInstance : IDisposable
{
    public IReadOnlyDictionary<Type, IProjectEvaluator> TypeMap => _typeMap;
    private readonly Dictionary<Type, IProjectEvaluator> _typeMap;
    public readonly CLContext Db;
    public readonly CLConfiguration Configuration;
    private bool _disposed;

    public CLInstance(Dictionary<Type, IProjectEvaluator> typeMap, CLContext db, CLConfiguration configuration)
    {
        _typeMap = typeMap;
        Db = db;
        Configuration = configuration;
    }

    public static async Task<CLInstance> CreateAsync(CLConfiguration configuration)
    {
        Dictionary<Type, IProjectEvaluator> typeMap = configuration.Evaluators.ToDictionary(v => v.GetType());
        var dbFac = new CLContextFactory();
        var db = dbFac.CreateDbContext(Array.Empty<string>());
        await db.Database.MigrateAsync();
        return new CLInstance(typeMap, db, configuration);
    }

    public readonly record struct DirectoryAddResult(bool Success, ProjectDirectoryModel Model);

    public Task<DirectoryAddResult> AddDirectoryAsync(string directory)
    {
        EnsureNotDisposed();
        return AddDirectoryInternalAsync(directory);
    }

    private async Task<DirectoryAddResult> AddDirectoryInternalAsync(string directory)
    {
        var v = new ProjectDirectoryModel { FullPath = Path.GetFullPath(directory), Projects = new HashSet<ProjectDirectoryProjectModel>(), RecordUpdateTime = DateTime.Now };
        var result = await Db.AddProjectDirectoryAsync(v);
        return new DirectoryAddResult(ReferenceEquals(v, result), result);
    }

    public Task<bool> RemoveDirectoryAsync(string directory)
    {
        EnsureNotDisposed();
        return Db.RemoveProjectDirectoryAsync(new ProjectDirectoryModel { FullPath = Path.GetFullPath(directory), Projects = new HashSet<ProjectDirectoryProjectModel>(), RecordUpdateTime = DateTime.Now });
    }

    public Task UpdateDirectoryAsync(ProjectDirectoryModel directory)
    {
        EnsureNotDisposed();
        return Db.UpdateProjectDirectoryProjectListAsync(directory, Configuration, Configuration.Evaluators);
    }

    public Task UpdateAllDirectoriesAsync()
    {
        EnsureNotDisposed();
        return UpdateAllDirectoriesInternalAsync();
    }

    private async Task UpdateAllDirectoriesInternalAsync()
    {
        foreach (var dir in Db.ProjectDirectories.ToList())
            await Db.UpdateProjectDirectoryProjectListAsync(dir, Configuration, Configuration.Evaluators);
    }

    public Task<RecentProjectModel> PushRecentProjectAsync(RecentProjectModel recentProject)
    {
        EnsureNotDisposed();
        return Db.PushRecentProjectAsync(Configuration, recentProject);
    }

    public Task<RecentProjectModel> PushRecentProjectAsync(BaseProjectModel project)
    {
        EnsureNotDisposed();
        return Db.PushRecentProjectAsync(Configuration, project);
    }

    public IProjectEvaluator? GetProjectEvaluator(BaseProjectModel project)
    {
        EnsureNotDisposed();
        var typeString = TypeTool.ParseTypeString(project.ProjectEvaluatorType);
        var type = typeString.Load();
        return _typeMap.TryGetValue(type, out var evaluator) ? evaluator : null;
    }

    public Task<ProjectLoadResult> LoadAsync(BaseProjectModel projectModel)
    {
        EnsureNotDisposed();
        var evaluator = GetProjectEvaluator(projectModel);
        if (evaluator == null) return Task.FromResult(ProjectLoadResult.Failure("Indecipherable Project", "This project is of an unknown type and cannot be opened"));
        var loader = evaluator.GetProjectLoader();
        return loader.TryLoadAsync(projectModel);
    }

    public string GetPlatformName(BaseProjectModel project)
    {
        EnsureNotDisposed();
        return GetProjectEvaluator(project)?.FriendlyPlatformName ?? "unknown";
    }

    public string GetDisplayFramework(BaseProjectModel project)
    {
        EnsureNotDisposed();
        return GetProjectEvaluator(project)?.GetDisplayFramework(project) ?? project.Framework;
    }

    private void EnsureNotDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(CLInstance));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Db.Dispose();
    }
}
