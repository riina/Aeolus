using System.Text.RegularExpressions;

namespace CrossLaunch.Unity;

// TODO switch to an implementation seeking project file first
public class UnitySupport : FolderProjectEvaluatorBase
{
    private static readonly Regex s_projectVersionRegex = new(@"m_EditorVersionWithRevision:\s*(?<EditorVersion>\S+)\s*\((?<Revision>\S+)\)");

    public override string FriendlyPlatformName => "Unity";

    public override async Task<EvaluatedProject?> EvaluateProjectAsync(string path, CLConfiguration configuration, CancellationToken cancellationToken = default)
    {
        string projectFile = Path.Combine(path, "ProjectSettings", "ProjectVersion.txt");
        if (!File.Exists(projectFile)) return null;
        string projectFileContent = await File.ReadAllTextAsync(projectFile, cancellationToken);
        if (s_projectVersionRegex.Match(projectFileContent) is not { Success: true } match) return null;
        return new EvaluatedProject(Path.GetFullPath(path), $"{match.Groups["EditorVersion"]}/{match.Groups["Revision"]}");
    }
}
