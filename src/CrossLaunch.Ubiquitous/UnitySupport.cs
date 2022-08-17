using CrossLaunch.Models;
using CrossLaunch.Ubiquitous.Projects;

namespace CrossLaunch.Ubiquitous;

// maybe switch to an implementation seeking project file first?
public class UnitySupport : FolderSupportBase<UnitySupport.Loader>
{
    public override string FriendlyPlatformName => "Unity";

    public override async Task<EvaluatedProject?> EvaluateProjectAsync(string path, CLConfiguration configuration, CancellationToken cancellationToken = default)
        => (await UnityProject.ParseAsync(path)).Result?.AsEvaluatedProject();

    public override string GetDisplayFramework(BaseProjectModel project)
        => UnityProject.TryGetDisplayFramework(project, out string? result) ? result : project.Framework;

    public class Loader : ParsedProjectLoader<UnityProject>
    {
        protected override async Task<ProjectParseResult<UnityProject>> ParseAsync(string path)
            => await UnityProject.ParseAsync(path);
    }
}
