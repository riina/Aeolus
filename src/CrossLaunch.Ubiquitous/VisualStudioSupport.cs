using CrossLaunch.Models;
using CrossLaunch.Ubiquitous.Projects;

namespace CrossLaunch.Ubiquitous;

public class VisualStudioSupport : FileSupportBase<VisualStudioProjectLoader>
{
    public override string FriendlyPlatformName => "Visual Studio";

    public override async Task<EvaluatedProject?> EvaluateProjectAsync(string path, CLConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var loadResult = await VisualStudioSolution.LoadAsync(path);
        return loadResult.Result is { } result ? new EvaluatedProject(Path.GetFullPath(path), result.FrameworkString) : null;
    }

    public override string GetDisplayFramework(BaseProjectModel project)
        => VisualStudioSolution.TryGetDisplayFramework(project, out string? result) ? result : project.Framework;
}

public class VisualStudioProjectLoader : IProjectLoader
{
    public async Task<ProjectLoadResult> TryLoadAsync(BaseProjectModel project, CLConfiguration configuration)
    {
        var loadResult = await VisualStudioSolution.LoadAsync(project.FullPath);
        if (loadResult.Result is not { } result) return loadResult.FailInfo?.AsProjectLoadResult() ?? ProjectLoadResult.Unknown;
        return await result.TryLoadAsync(configuration);
    }
}
