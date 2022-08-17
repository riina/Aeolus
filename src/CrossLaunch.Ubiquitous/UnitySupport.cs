using CrossLaunch.Models;
using CrossLaunch.Ubiquitous.Projects;

namespace CrossLaunch.Ubiquitous;

// maybe switch to an implementation seeking project file first?
public class UnitySupport : FolderSupportBase<UnityProjectLoader>
{
    public override string FriendlyPlatformName => "Unity";

    public override async Task<EvaluatedProject?> EvaluateProjectAsync(string path, CLConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var loadResult = await UnityProject.LoadAsync(path);
        return loadResult.Result is { } result ? new EvaluatedProject(Path.GetFullPath(path), result.FrameworkString) : null;
    }

    public override string GetDisplayFramework(BaseProjectModel project)
        => UnityProject.TryGetDisplayFramework(project, out string? result) ? result : project.Framework;
}

public class UnityProjectLoader : ProjectLoaderBase
{
    public override async Task<ProjectLoadResult> TryLoadAsync(BaseProjectModel project, CLConfiguration configuration)
    {
        var loadResult = await UnityProject.LoadAsync(project.FullPath);
        if (loadResult.Result is not { } result) return loadResult.FailInfo?.AsProjectLoadResult() ?? ProjectLoadResult.Unknown;
        return await result.TryLoadAsync(configuration);
    }
}
