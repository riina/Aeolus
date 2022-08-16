using System.Runtime.CompilerServices;

namespace CrossLaunch;

public abstract class SupportBase<T> : ProjectEvaluatorBase where T : IProjectLoader, new()
{
    public override IProjectLoader GetProjectLoader() => new T();
}

public abstract class FolderSupportBase<T> : SupportBase<T> where T : IProjectLoader, new()
{
    public override async IAsyncEnumerable<EvaluatedProject> FindProjectsAsync(string path, CLConfiguration configuration, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var (itemIsFile, itemPath) in Recurser.Recurse(new[] { path }, configuration.MaxDepth))
        {
            if (itemIsFile) continue;
            var evaluated = await EvaluateProjectAsync(itemPath, configuration, cancellationToken);
            if (evaluated != null) yield return evaluated;
        }
    }
}

public abstract class FileSupportBase<T> : SupportBase<T> where T : IProjectLoader, new()
{
    public override async IAsyncEnumerable<EvaluatedProject> FindProjectsAsync(string path, CLConfiguration configuration, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var (itemIsFile, itemPath) in Recurser.Recurse(new[] { path }, configuration.MaxDepth))
        {
            if (!itemIsFile) continue;
            var evaluated = await EvaluateProjectAsync(itemPath, configuration, cancellationToken);
            if (evaluated != null) yield return evaluated;
        }
    }
}
