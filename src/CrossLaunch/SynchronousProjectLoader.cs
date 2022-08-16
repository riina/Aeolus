using CrossLaunch.Models;

namespace CrossLaunch;

public abstract class SynchronousProjectLoader : IProjectLoader
{
    public Task<ProjectLoadResult> TryLoadAsync(BaseProjectModel project, CLConfiguration configuration)
        => Task.FromResult(TryLoad(project, configuration));

    public abstract ProjectLoadResult TryLoad(BaseProjectModel project, CLConfiguration configuration);
}
