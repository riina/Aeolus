using CrossLaunch.Models;
using CrossLaunch.Ubiquitous.Projects;

namespace CrossLaunch.Ubiquitous;

public class VisualStudioSupport : FileSupportBase<VisualStudioSupport.Loader>
{
    public override string FriendlyPlatformName => "Visual Studio";

    public override async Task<EvaluatedProject?> EvaluateProjectAsync(string path, CLConfiguration configuration, CancellationToken cancellationToken = default)
        => (await VisualStudioSolution.ParseAsync(path)).Result?.AsEvaluatedProject();

    public override string GetDisplayFramework(BaseProjectModel project)
        => VisualStudioSolution.TryGetDisplayFramework(project, out string? result) ? result : project.Framework;

    public class Loader : ParsedProjectLoader<VisualStudioSolution>
    {
        protected override async Task<ProjectParseResult<VisualStudioSolution>> ParseAsync(string path)
            => await VisualStudioSolution.ParseAsync(path);
    }
}
