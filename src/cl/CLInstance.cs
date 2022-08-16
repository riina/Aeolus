using System.Diagnostics.CodeAnalysis;
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
        return loader.TryLoadAsync(projectModel, Configuration);
    }

    public string GetRelativePathInProjectFolder(ProjectDirectoryProjectModel project)
    {
        EnsureNotDisposed();
        return GetRelativePathInProjectFolderInternal(project, project.ProjectDirectory.FullPath);
    }

    public string GetRelativePathInProjectFolder(BaseProjectModel project, string projectFolder)
    {
        EnsureNotDisposed();
        return GetRelativePathInProjectFolderInternal(project, projectFolder);
    }

    private string GetRelativePathInProjectFolderInternal(BaseProjectModel project, string projectFolder)
    {
        return Path.GetRelativePath(projectFolder, project.FullPath);
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

    public string GetShorthand(ProjectDirectoryProjectModel project, CLDirectoryPairs pairs)
    {
        return $"{pairs.From[project.ProjectDirectory.FullPath]}:{GetRelativePathInProjectFolder(project)}";
    }

    public CLDirectoryPairs CreateUnambiguousProjectDirectoryAliases()
    {
        EnsureNotDisposed();
        string[] keys = Db.ProjectDirectories.Select(v => v.FullPath).ToArray();
        (string src, string main, string sub)[] arr = new (string src, string main, string sub)[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            ref var v = ref arr[i];
            string key = keys[i];
            v.src = key;
            v.main = Path.GetDirectoryName(key) ?? "";
            v.sub = Path.GetFileName(key);
        }
        int ch = 0;
        do
        {
            for (int i = 0; i < arr.Length; i++)
            {
                ref var v = ref arr[i];
                int mIdx = -1;
                for (int j = 0; j < i; j++)
                    if (arr[j].sub.Equals(v.sub))
                    {
                        mIdx = j;
                        break;
                    }
                if (mIdx == -1)
                    for (int j = i + 1; j < arr.Length; j++)
                        if (arr[j].sub.Equals(v.sub))
                        {
                            mIdx = j;
                            break;
                        }
                if (mIdx != -1)
                {
                    ch++;
                    string? main2 = Path.GetDirectoryName(v.main);
                    if (main2 != null)
                    {
                        v.main = main2;
                        v.sub = Path.Combine(Path.GetFileName(v.main), v.sub);
                    }
                    else
                    {
                        // since db is keyed by full path, *should* be unique and never touched again
                        v.main = "";
                        v.sub = v.src;
                    }
                }
            }
        } while (ch != 0);
        return new CLDirectoryPairs(arr.ToDictionary(v => v.src, v => v.sub), arr.ToDictionary(v => v.sub, v => v.src));
    }

    public Task<BaseProjectModel?> FindProjectAsync(string key, CLDirectoryPairs? pairs = null)
    {
        EnsureNotDisposed();
        return FindProjectInternalAsync(key, pairs);
    }

    public async Task<BaseProjectModel?> FindProjectInternalAsync(string key, CLDirectoryPairs? pairs = null)
    {
        BaseProjectModel? project;
        string fullPath = Path.GetFullPath(key);
        project = Db.ProjectDirectoryProjects.FirstOrDefault(v => v.Nickname == key);
        if (project == null && TrySplitProjectShorthand(key, out string? pairValue, out string? subDir))
        {
            if (pairs != null && pairs.To.TryGetValue(pairValue, out string? pairKey))
            {
                project = await Db.ProjectDirectoryProjects.FindAsync(Path.GetFullPath(Path.Combine(pairKey, subDir)));
            }
        }
        else
            project ??= await Db.ProjectDirectoryProjects.FindAsync(fullPath);
        return project;
    }

    private static bool TrySplitProjectShorthand(string key, [NotNullWhen(true)] out string? pairValue, [NotNullWhen(true)] out string? subDir)
    {
        ReadOnlySpan<char> k = key;
        int index = k.IndexOf(':');
        if (index != -1)
        {
            // don't trim, paths *could* have lead/trail ws
            pairValue = new string(k[..index]);
            subDir = new string(k[(index + 1)..]);
            return true;
        }
        pairValue = null;
        subDir = null;
        return false;
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
