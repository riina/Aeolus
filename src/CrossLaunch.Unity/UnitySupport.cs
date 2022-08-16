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

    public override string GetDisplayFramework(BaseProjectModel project)
    {
        try
        {
            var unityVersion = UnityVersion.FromCombined(project.Framework);
            return unityVersion.EditorVersion;
        }
        catch
        {
            return project.Framework;
        }
    }
}

public class UnityProjectLoader : IProjectLoader
{
    public Task<ProjectLoadResult> TryLoadAsync(BaseProjectModel project) => Task.FromResult(TryLoad(project));

    public ProjectLoadResult TryLoad(BaseProjectModel project)
    {
        var version = UnityVersion.FromCombined(project.Framework);
        string[] searchLocations;
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
        else return new ProjectLoadResult(false, new ProjectLoadFailInfo("Unsupported OS", "This operating system is not supported", Array.Empty<ProjectLoadFailRemediation>()));
        string? first = searchLocations.FirstOrDefault(File.Exists);
        if (first == null)
        {
            string message = @$"Unity Editor version {version.EditorVersion} is required for this project but is not currently installed.

The required Unity Editor version can be installed through Unity Hub or from the Unity Download Archive.

https://unity3d.com/get-unity/download/archive";
            if (OperatingSystem.IsMacOS())
                message += @"

Warning: Due to unityhub:// link limitations and Unity Hub limitations, Apple Silicon editors may not be installable except through .dmg images from the Unity Download Archive.";
            List<ProjectLoadFailRemediation> remediations = new();
            remediations.Add(new ProjectLoadFailRemediation("Open Unity Download Archive", $"Open the Unity Download Archive in a browser and install Unity Editor {version.EditorVersion}.", ProcessUtils.GetUriCallback("https://unity3d.com/get-unity/download/archive")));
            if (hubLocations.Any(File.Exists))
                remediations.Insert(0, new ProjectLoadFailRemediation("Open Unity Hub", $"Open Unity Hub with Unity Editor {version.EditorVersion} selected for install.", ProcessUtils.GetUriCallback($"unityhub://{version.EditorVersion}/{version.Revision}")));
            return new ProjectLoadResult(false, new ProjectLoadFailInfo("Editor Not Installed", message, remediations.ToArray()));
        }
        ProcessUtils.Start(first, "-projectPath", project.FullPath);
        return new ProjectLoadResult(true, null);
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
