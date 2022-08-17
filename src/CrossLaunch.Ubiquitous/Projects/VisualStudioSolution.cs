using System.Diagnostics.CodeAnalysis;
using System.Text;
using CrossLaunch.Models;
using CrossLaunch.Ubiquitous.Formats;

namespace CrossLaunch.Ubiquitous.Projects;

public record VisualStudioSolution(VisualStudioSolutionFile SolutionFile) : ProjectBase
{
    public override string FrameworkString => $"{SolutionFile.MinimumVisualStudioVersion}/{SolutionFile.VisualStudioVersion}";

    public static async Task<ProjectParseResult<VisualStudioSolution>> LoadAsync(string path)
    {
        if (!".sln".Equals(Path.GetExtension(path), StringComparison.InvariantCultureIgnoreCase)) return ProjectParseResult<VisualStudioSolution>.InvalidExtension;
        try
        {
            using var stream = File.OpenText(path);
            var solution = await VisualStudioSolutionFile.LoadAsync(stream);
            var sb = new StringBuilder();
            sb.Append(solution.MinimumVisualStudioVersion).Append('/').Append(solution.VisualStudioVersion);
            return new ProjectParseResult<VisualStudioSolution>(new VisualStudioSolution(solution));
        }
        catch (InvalidDataException)
        {
            return ProjectParseResult<VisualStudioSolution>.InvalidFile;
        }
    }

    public static bool TryGetDisplayFramework(BaseProjectModel project, [NotNullWhen(true)] out string? result)
    {
        ReadOnlySpan<char> slice = project.Framework;
        int index = slice.IndexOf('/');
        if (index != -1)
        {
            slice = slice[..index];
            int index2 = slice.IndexOf('.');
            if (index2 != -1)
            {
                result = new string(slice[..index2]);
                return true;
            }
        }
        result = null;
        return false;
    }
}
