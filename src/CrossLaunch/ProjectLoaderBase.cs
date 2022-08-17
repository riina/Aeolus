using CrossLaunch.Models;

namespace CrossLaunch;

public abstract class ProjectLoaderBase : IProjectLoader
{
    public abstract Task<ProjectLoadResult> TryLoadAsync(BaseProjectModel project, CLConfiguration configuration);
}

public abstract class SynchronousProjectLoader : ProjectLoaderBase
{
    public override Task<ProjectLoadResult> TryLoadAsync(BaseProjectModel project, CLConfiguration configuration)
        => Task.FromResult(TryLoad(project, configuration));

    public abstract ProjectLoadResult TryLoad(BaseProjectModel project, CLConfiguration configuration);
}
