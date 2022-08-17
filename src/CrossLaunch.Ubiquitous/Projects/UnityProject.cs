using System.Diagnostics.CodeAnalysis;
using CrossLaunch.Models;
using CrossLaunch.Ubiquitous.Formats;

namespace CrossLaunch.Ubiquitous.Projects;

public record UnityProject(UnityProjectVersionFile ProjectVersionFile) : ProjectBase
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
            return new ProjectParseResult<UnityProject>(new UnityProject(projectVersionFile));
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
}
