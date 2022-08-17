using CrossLaunch.Models;
using CrossLaunch.Ubiquitous.Projects;

namespace CrossLaunch.Ubiquitous;

// maybe switch to an implementation seeking project file first?
public class UnitySupport : FolderSupportBase<UnityProjectLoader>
{
    public override string FriendlyPlatformName => "Unity";

    public override async Task<EvaluatedProject?> EvaluateProjectAsync(string path, CLConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var loadResult = await UnityProject.LoadAsync(path);
        return loadResult.Result is { } result ? new EvaluatedProject(Path.GetFullPath(path), result.FrameworkString) : null;
    }

    public override string GetDisplayFramework(BaseProjectModel project)
        => UnityProject.TryGetDisplayFramework(project, out string? result) ? result : project.Framework;
}

public class UnityProjectLoader : ProjectLoaderBase
{
    public override async Task<ProjectLoadResult> TryLoadAsync(BaseProjectModel project, CLConfiguration configuration)
    {
        var loadResult = await UnityProject.LoadAsync(project.FullPath);
        if (loadResult.Result is not { } result) return loadResult.FailInfo?.AsProjectLoadResult() ?? ProjectLoadResult.Unknown;
        var version = result.ProjectVersionFile.Version;
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
            remediations.Add(new ProjectLoadFailRemediation("Open Unity Download Archive in Browser", @$"Install Unity Editor {version.EditorVersion} from the Unity Download Archive.
https://unity3d.com/get-unity/download/archive", ProcessUtils.GetUriCallback("https://unity3d.com/get-unity/download/archive")));
            if (hubLocations.Any(File.Exists))
                remediations.Insert(0, new ProjectLoadFailRemediation("Install With Hub", @$"Open Unity Hub with Unity Editor {version.EditorVersion} selected for install.
unityhub://{version.EditorVersion}/{version.Revision}", ProcessUtils.GetUriCallback($"unityhub://{version.EditorVersion}/{version.Revision}")));
            return ProjectLoadResult.Failure($"Unity Editor {version.EditorVersion} Not Installed", message, remediations.ToArray());
        }
        ProcessUtils.Start(first, "-projectPath", project.FullPath);
        return ProjectLoadResult.Successful;
    }
}
