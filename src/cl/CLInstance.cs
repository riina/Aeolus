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

    public async Task AddDirectoryAsync(string directory)
    {
        await Db.AddProjectDirectoryAsync(new ProjectDirectoryModel { FullPath = directory, Projects = new HashSet<ProjectDirectoryProjectModel>(), RecordUpdateTime = DateTime.Now });
    }

    public async Task RemoveDirectoryAsync(string directory)
    {
        await Db.RemoveProjectDirectoryAsync(new ProjectDirectoryModel { FullPath = directory, Projects = new HashSet<ProjectDirectoryProjectModel>(), RecordUpdateTime = DateTime.Now });
    }

    public async Task UpdateAllDirectoriesAsync()
    {
        foreach (var dir in Db.ProjectDirectories.ToList())
            await Db.UpdateProjectDirectoryProjectListAsync(dir, Configuration, Configuration.Evaluators);
    }

    public IProjectEvaluator? GetProjectEvaluator(BaseProjectModel project)
    {
        var typeString = TypeTool.ParseTypeString(project.ProjectEvaluatorType);
        var type = typeString.Load();
        return _typeMap.TryGetValue(type, out var evaluator) ? evaluator : null;
    }

    public Task<ProjectLoadResult> LoadAsync(BaseProjectModel projectModel)
    {
        var evaluator = GetProjectEvaluator(projectModel);
        if (evaluator == null)
            // TODO
            return Task.FromResult(new ProjectLoadResult(false, new ProjectLoadFailInfo("TODO", "TODO", Array.Empty<ProjectLoadFailRemediation>())));
        var loader = evaluator.GetProjectLoader();
        return loader.TryLoadAsync(projectModel);
    }

    public string GetPlatformName(BaseProjectModel project)
    {
        return GetProjectEvaluator(project)?.FriendlyPlatformName ?? "unknown";
    }

    public string GetDisplayFramework(BaseProjectModel project)
    {
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
