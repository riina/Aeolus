using CrossLaunch.Models;

namespace CrossLaunch;

public interface IProjectLoader
{
    Task<ProjectLoadResult> TryLoadAsync(BaseProjectModel project, CLConfiguration configuration);
}
