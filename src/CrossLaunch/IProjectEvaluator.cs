using CrossLaunch.Models;

namespace CrossLaunch;

public interface IProjectEvaluator
{
    string FriendlyPlatformName { get; }

    Task<EvaluatedProject?> EvaluateProjectAsync(string path, CLConfiguration configuration, CancellationToken cancellationToken = default);

    IAsyncEnumerable<EvaluatedProject> FindProjectsAsync(string path, CLConfiguration configuration, CancellationToken cancellationToken = default);

    IProjectLoader GetProjectLoader();

    string GetDisplayFramework(BaseProjectModel project);
}
