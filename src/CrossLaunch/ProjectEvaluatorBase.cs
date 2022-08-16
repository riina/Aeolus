using System.Runtime.CompilerServices;
using CrossLaunch.Models;

namespace CrossLaunch;

public abstract class ProjectEvaluatorBase : IProjectEvaluator
{
    public abstract string FriendlyPlatformName { get; }

    public abstract Task<EvaluatedProject?> EvaluateProjectAsync(string path, CLConfiguration configuration, CancellationToken cancellationToken = default);

    public abstract IAsyncEnumerable<EvaluatedProject> FindProjectsAsync(string path, CLConfiguration configuration, CancellationToken cancellationToken = default);

    public abstract IProjectLoader GetProjectLoader();

    public abstract string GetDisplayFramework(BaseProjectModel project);
}

public abstract class FolderProjectEvaluatorBase : ProjectEvaluatorBase
{
    public override async IAsyncEnumerable<EvaluatedProject> FindProjectsAsync(string path, CLConfiguration configuration, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var (itemIsFile, itemPath) in Recurser.Recurse(new[] { path }, configuration.MaxDepthWithOverride))
        {
            if (itemIsFile) continue;
            var evaluated = await EvaluateProjectAsync(itemPath, configuration, cancellationToken);
            if (evaluated != null) yield return evaluated;
        }
    }
}

public abstract class FileProjectEvaluatorBase : ProjectEvaluatorBase
{
    public override async IAsyncEnumerable<EvaluatedProject> FindProjectsAsync(string path, CLConfiguration configuration, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var (itemIsFile, itemPath) in Recurser.Recurse(new[] { path }, configuration.MaxDepthWithOverride))
        {
            if (!itemIsFile) continue;
            var evaluated = await EvaluateProjectAsync(itemPath, configuration, cancellationToken);
            if (evaluated != null) yield return evaluated;
        }
    }
}
