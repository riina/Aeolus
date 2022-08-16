using System.Text;
using CrossLaunch.Models;

namespace CrossLaunch.Ubiquitous;

public class VisualStudioSupport : FileSupportBase<VisualStudioProjectLoader>
{
    public override string FriendlyPlatformName => "Visual Studio";

    public override async Task<EvaluatedProject?> EvaluateProjectAsync(string path, CLConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (!".sln".Equals(Path.GetExtension(path), StringComparison.InvariantCultureIgnoreCase)) return null;
        return EvaluateProject(path, await File.ReadAllLinesAsync(path, cancellationToken));
    }

    private EvaluatedProject? EvaluateProject(string path, string[] projectFile)
    {
        int i;
        for (i = 0; i < projectFile.Length; i++)
        {
            if (projectFile[i].StartsWith("Microsoft Visual Studio Solution File")) break;
        }
        if (i == projectFile.Length) return null;
        ReadOnlySpan<char> visualStudioVersion = ReadOnlySpan<char>.Empty;
        ReadOnlySpan<char> minimumVisualStudioVersion = ReadOnlySpan<char>.Empty;
        for (; i < projectFile.Length; i++)
        {
            ReadOnlySpan<char> line = projectFile[i];
            var l = line.TrimStart();
            if (l.StartsWith("#")) continue;
            if (TryGetKeyValue(l, "VisualStudioVersion", out var tmpVisualStudioVersion)) visualStudioVersion = tmpVisualStudioVersion;
            if (TryGetKeyValue(l, "MinimumVisualStudioVersion", out var tmpMinimumVisualStudioVersion)) minimumVisualStudioVersion = tmpMinimumVisualStudioVersion;
            if (visualStudioVersion.Length != 0 && minimumVisualStudioVersion.Length != 0) break;
        }
        if (visualStudioVersion.Length != 0 && minimumVisualStudioVersion.Length != 0)
        {
            var sb = new StringBuilder();
            sb.Append(minimumVisualStudioVersion).Append('/').Append(visualStudioVersion);
            return new EvaluatedProject(Path.GetFullPath(path), sb.ToString());
        }
        return null;
    }

    private static bool TryGetKeyValue(ReadOnlySpan<char> source, ReadOnlySpan<char> pattern, out ReadOnlySpan<char> result)
    {
        source = source.TrimStart();
        if (source.StartsWith(pattern))
        {
            source = source[pattern.Length..].TrimStart();
            if (source.StartsWith("="))
            {
                result = source[1..].Trim();
                return true;
            }
        }
        result = ReadOnlySpan<char>.Empty;
        return false;
    }

    public override string GetDisplayFramework(BaseProjectModel project)
    {
        ReadOnlySpan<char> slice = project.Framework;
        int index = slice.IndexOf('/');
        if (index == -1) return project.Framework;
        slice = slice[..index];
        int index2 = slice.IndexOf('.');
        if (index2 == -1) return project.Framework;
        return new string(slice[..index2]);
    }
}

public class VisualStudioProjectLoader : IProjectLoader
{
    public Task<ProjectLoadResult> TryLoadAsync(BaseProjectModel project, CLConfiguration configuration)
    {
        // TODO
        return Task.FromResult(ProjectLoadResult.Failure("Not Supported", "Loading Visual Studio projects is not yet supported"));
    }
}
