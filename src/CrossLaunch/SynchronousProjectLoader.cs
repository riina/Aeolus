using CrossLaunch.Models;

namespace CrossLaunch;

public abstract class SynchronousProjectLoader : IProjectLoader
{
    public Task<ProjectLoadResult> TryLoadAsync(BaseProjectModel project) => Task.FromResult(TryLoad(project));

    public abstract ProjectLoadResult TryLoad(BaseProjectModel project);
}
