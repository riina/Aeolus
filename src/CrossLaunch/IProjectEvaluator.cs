namespace CrossLaunch;

public interface IProjectEvaluator
{
    string FriendlyPlatformName { get; }

    Task<EvaluatedProject?> EvaluateProjectAsync(string path, CLConfiguration configuration, CancellationToken cancellationToken = default);

    IAsyncEnumerable<EvaluatedProject> FindProjectsAsync(string path, CLConfiguration configuration, CancellationToken cancellationToken = default);
}
