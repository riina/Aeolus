namespace CrossLaunch;

public interface IProjectEvaluator
{
    Task<EvaluatedProject?> EvaluateProjectAsync(string path, CancellationToken cancellationToken = default);

    IAsyncEnumerable<EvaluatedProject> FindProjectsAsync(string path, CancellationToken cancellationToken = default);
}
