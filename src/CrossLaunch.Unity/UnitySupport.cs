using System.Text.RegularExpressions;
using CrossLaunch.Models;

namespace CrossLaunch.Unity;

// maybe switch to an implementation seeking project file first?
public class UnitySupport : FolderSupportBase<UnityProjectLoader>
{
    private static readonly Regex s_projectVersionRegex = new(@"m_EditorVersionWithRevision:\s*(?<EditorVersion>\S+)\s*\((?<Revision>\S+)\)");

    public override string FriendlyPlatformName => "Unity";

    public override async Task<EvaluatedProject?> EvaluateProjectAsync(string path, CLConfiguration configuration, CancellationToken cancellationToken = default)
    {
        string projectFile = Path.Combine(path, "ProjectSettings", "ProjectVersion.txt");
        if (!File.Exists(projectFile)) return null;
        return s_projectVersionRegex.Match(await File.ReadAllTextAsync(projectFile, cancellationToken)) is { Success: true } match
            ? new EvaluatedProject(Path.GetFullPath(path), $"{match.Groups["EditorVersion"]}/{match.Groups["Revision"]}")
            : null;
    }

    public override string GetDisplayFramework(BaseProjectModel project) =>
        UnityVersion.TryParseFromCombined(project.Framework, out var unityVersion) ? unityVersion.EditorVersion : project.Framework;
}

public class UnityProjectLoader : SynchronousProjectLoader
{
    public override ProjectLoadResult TryLoad(BaseProjectModel project)
    {
        if (!UnityVersion.TryParseFromCombined(project.Framework, out var version))
            return ProjectLoadResult.Failure("Invalid Framework ID", $"Could not process framework ID \"{project.Framework}\"");
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
        else return ProjectLoadResult.Failure("Unsupported OS", "This operating system is not supported");
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
            return ProjectLoadResult.Failure($"Unity Editor {version.EditorVersion} Not Installed", message, remediations.ToArray());
        }
        ProcessUtils.Start(first, "-projectPath", project.FullPath);
        return ProjectLoadResult.Successful;
    }
}

internal readonly record struct UnityVersion(string EditorVersion, string Revision)
{
    public static bool TryParseFromCombined(string combined, out UnityVersion parsed)
    {
        int index = combined.LastIndexOf('/');
        if (index == -1)
        {
            parsed = default;
            return false;
        }
        parsed = new UnityVersion(combined[..index], combined[(index + 1)..]);
        return true;
    }
}
