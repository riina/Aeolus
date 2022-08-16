using System.Text.RegularExpressions;
using CrossLaunch.Models;

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

    public override IProjectLoader GetProjectLoader() => new UnityProjectLoader();
}

public class UnityProjectLoader : IProjectLoader
{
    public Task<ProjectLoadResult> TryLoadAsync(BaseProjectModel project) => Task.FromResult(TryLoad(project));

    public ProjectLoadResult TryLoad(BaseProjectModel project)
    {
        string[] searchLocations;
        var version = UnityVersion.FromCombined(project.Framework);
        string[] hubLocations;
        if (OperatingSystem.IsWindows())
        {
            const string editorFormat = @"Editor\Unity.exe";
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            searchLocations = new[] { Path.Combine(programFiles, @"Unity\Hub\Editor", version.EditorVersion, editorFormat) };
            hubLocations = new[] { Path.Combine(programFiles, @"Unity Hub\Unity Hub.exe") };
        }
        else if (OperatingSystem.IsMacOS())
        {
            const string editorFormat = "Unity.app/Contents/MacOS/Unity";
            searchLocations = new[] { Path.Combine("/Applications/Unity/Hub/Editor", version.EditorVersion, editorFormat) };
            hubLocations = new[] { "/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" };
        }
        else return new ProjectLoadResult(false, new ProjectLoadFailInfo("Unsupported OS", "This operating system is not supported", null));
        string? first = searchLocations.FirstOrDefault(File.Exists);
        if (first == null)
        {
            string message = @$"Unity Editor version {version.EditorVersion} is required for this project but is not currently installed.
The required Unity Editor version can be installed through Unity Hub or from the Unity Download Archive.
https://unity3d.com/get-unity/download/archive";
            if (OperatingSystem.IsMacOS())
                message += @"
Warning: Due to unityhub:// link limitations and Unity Hub limitations, Apple Silicon editors may not be installable except through .dmg images from the Unity Download Archive.";
            ProjectLoadFailRemediation remediation;
            remediation = hubLocations.Any(File.Exists)
                ? new ProjectLoadFailRemediation("Open Unity Hub", "Unity Hub can be opened with the required editor selected for install.", GetUnityHubDownloadLinkCallback(version))
                : new ProjectLoadFailRemediation("Open Unity Download Archive", "A browser can be opened and pointed to the Unity Download Archive.", OpenUnityDownloadArchiveLinkAsync);
            return new ProjectLoadResult(false, new ProjectLoadFailInfo("Editor Not Installed", message, remediation));
        }
        ProcessUtils.Start(first, "-projectPath", project.FullPath);
        return new ProjectLoadResult(false, new ProjectLoadFailInfo("Unknown", "Unknown", null));
    }

    private static Func<Task> GetUnityHubDownloadLinkCallback(UnityVersion unityVersion) => () =>
    {
        ProcessUtils.StartUri($"unityhub://{unityVersion.EditorVersion}/{unityVersion.Revision}");
        return Task.CompletedTask;
    };

    public static Task OpenUnityDownloadArchiveLinkAsync()
    {
        ProcessUtils.StartUri("https://unity3d.com/get-unity/download/archive");
        return Task.CompletedTask;
    }
}

internal readonly record struct UnityVersion(string EditorVersion, string Revision)
{
    public static UnityVersion FromCombined(string combined)
    {
        int index = combined.LastIndexOf('/');
        if (index == -1) throw new ArgumentException("Invalid combined format");
        return new UnityVersion(combined[..index], combined[(index + 1)..]);
    }

    public string Combined => $"{EditorVersion}/{Revision}";
}
