using System.Diagnostics.CodeAnalysis;
using CrossLaunch.Models;
using CrossLaunch.Ubiquitous.Formats;

namespace CrossLaunch.Ubiquitous.Projects;

public record UnityProject(string FullPath, UnityProjectVersionFile ProjectVersionFile) : ProjectBase(FullPath)
{
    public override string FrameworkString => $"{ProjectVersionFile.Version.EditorVersion}/{ProjectVersionFile.Version.Revision}";

    public static async Task<ProjectParseResult<UnityProject>> LoadAsync(string path)
    {
        string projectFile = Path.Combine(path, "ProjectSettings", "ProjectVersion.txt");
        if (!File.Exists(projectFile)) return ProjectParseResult<UnityProject>.Missing;
        try
        {
            using var stream = File.OpenText(projectFile);
            var projectVersionFile = await UnityProjectVersionFile.LoadAsync(stream);
            return new ProjectParseResult<UnityProject>(new UnityProject(Path.GetFullPath(path), projectVersionFile));
        }
        catch (InvalidDataException)
        {
            return ProjectParseResult<UnityProject>.InvalidFile;
        }
    }

    public static bool TryGetDisplayFramework(BaseProjectModel project, [NotNullWhen(true)] out string? result)
    {
        if (UnityVersion.TryParseFromCombined(project.Framework, out var unityVersion))
        {
            result = unityVersion.EditorVersion;
            return true;
        }
        result = null;
        return false;
    }

    public override Task<ProjectLoadResult> TryLoadAsync(CLConfiguration configuration)
    {
        var version = ProjectVersionFile.Version;
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
        else return Task.FromResult(ProjectLoadResult.Failure("Unsupported OS", "This operating system is not supported"));
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
            return Task.FromResult(ProjectLoadResult.Failure($"Unity Editor {version.EditorVersion} Not Installed", message, remediations.ToArray()));
        }
        ProcessUtils.Start(first, "-projectPath", FullPath);
        return Task.FromResult(ProjectLoadResult.Successful);
    }
}
