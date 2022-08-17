using CrossLaunch.Ubiquitous.Formats;

namespace CrossLaunch.Ubiquitous;

public record UnityProject(UnityProjectVersionFile ProjectVersionFile)
{
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
}
