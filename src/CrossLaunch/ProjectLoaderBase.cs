using CrossLaunch.Models;

namespace CrossLaunch;

public abstract class ProjectLoaderBase : IProjectLoader
{
    public abstract Task<ProjectLoadResult> TryLoadAsync(BaseProjectModel project, CLConfiguration configuration);
}

public abstract class ParsedProjectLoader<T> : ProjectLoaderBase where T : ProjectBase
{
    protected abstract Task<ProjectParseResult<T>> ParseAsync(string path);

    public override async Task<ProjectLoadResult> TryLoadAsync(BaseProjectModel project, CLConfiguration configuration)
    {
        var loadResult = await ParseAsync(project.FullPath);
        if (loadResult.Result is not { } result) return loadResult.FailInfo?.AsProjectLoadResult() ?? ProjectLoadResult.Unknown;
        return await result.TryLoadAsync(configuration);
    }
}
