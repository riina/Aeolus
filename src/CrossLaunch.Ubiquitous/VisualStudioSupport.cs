using System.Diagnostics.CodeAnalysis;
using System.Text;
using CrossLaunch.Models;

namespace CrossLaunch.Ubiquitous;

public class VisualStudioSupport : FileSupportBase<VisualStudioProjectLoader>
{
    public override string FriendlyPlatformName => "Visual Studio";

    public override Task<EvaluatedProject?> EvaluateProjectAsync(string path, CLConfiguration configuration, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(".sln".Equals(Path.GetExtension(path), StringComparison.InvariantCultureIgnoreCase) ? EvaluateProject(path) : null);
    }

    private static EvaluatedProject? EvaluateProject(string path)
    {
        try
        {
            using var stream = File.OpenText(path);
            var solution = VSSolutionFile.Load(stream);
            var sb = new StringBuilder();
            sb.Append(solution.MinimumVisualStudioVersion).Append('/').Append(solution.VisualStudioVersion);
            return new EvaluatedProject(Path.GetFullPath(path), sb.ToString());
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    public override string GetDisplayFramework(BaseProjectModel project)
    {
        return VSSolutionFile.TryGetMinimumVisualStudio(project, out string? result) ? result : project.Framework;
    }
}

public class VisualStudioProjectLoader : IProjectLoader
{
    public Task<ProjectLoadResult> TryLoadAsync(BaseProjectModel project, CLConfiguration configuration)
    {
        if (!VSSolutionFile.TryGetMinimumVisualStudio(project, out string? framework)) return Task.FromResult(ProjectLoadResult.BadFrameworkId(project.Framework));
        // TODO parse out solution into project references
        var remediations = new List<ProjectLoadFailRemediation>();
        if (configuration.TryGetFlag("visualstudio.rider.enable", out bool riderEnabled))
        {
            // TODO check projects for rider compat (just check Sdk), try loading if valid
            remediations.Add(new ProjectLoadFailRemediation("Get JetBrains Rider", @"Install JetBrains Rider, a feature-rich proprietary IDE primarily for .NET development.
https://www.jetbrains.com/rider/", ProcessUtils.GetUriCallback("https://www.jetbrains.com/rider/")));
        }
        if (configuration.TryGetFlag("visualstudio.vscode.enable", out bool vscodeEnabled))
        {
            // TODO try loading through vscode
            remediations.Add(new ProjectLoadFailRemediation("Get Visual Studio Code", @"Install Visual Studio Code from Microsoft Corporation, a lightweight proprietary code editor.
https://code.visualstudio.com/", ProcessUtils.GetUriCallback("https://code.visualstudio.com/")));
        }
        if (OperatingSystem.IsWindows())
        {
            // TODO load through visual studio (e.g. vswhere)
            remediations.Add(new ProjectLoadFailRemediation("Get Visual Studio", @"Install Visual Studio from Microsoft Corporation, a feature-rich proprietary IDE.
https://visualstudio.microsoft.com/vs/", ProcessUtils.GetUriCallback("https://visualstudio.microsoft.com/vs/")));
        }
        else if (OperatingSystem.IsMacOS())
        {
            // TODO load through vs for mac
            remediations.Add(new ProjectLoadFailRemediation("Get Visual Studio for Mac", @"Install Visual Studio for Mac from Microsoft Corporation, a proprietary IDE primarily for .NET and Xamarin development.
https://visualstudio.microsoft.com/vs/mac/", ProcessUtils.GetUriCallback("https://visualstudio.microsoft.com/vs/mac/")));
        }
        return Task.FromResult(ProjectLoadResult.Failure("No Valid Program Found", @"Failed to identify software capable of opening this Visual Studio solution file.

A program such as Visual Studio or Rider must be installed.", remediations.ToArray()));
    }
}

public class VSSolutionFile
{
    public readonly string MinimumVisualStudioVersion;
    public readonly string VisualStudioVersion;

    public VSSolutionFile(string minimumVisualStudioVersion, string visualStudioVersion)
    {
        MinimumVisualStudioVersion = minimumVisualStudioVersion;
        VisualStudioVersion = visualStudioVersion;
    }

    public static VSSolutionFile Load(TextReader reader)
    {
        string? readLine;
        while ((readLine = reader.ReadLine()) != null)
            if (readLine.StartsWith("Microsoft Visual Studio Solution File"))
                break;
        if (readLine == null) throw new InvalidDataException("Missing header");
        ReadOnlySpan<char> visualStudioVersion = ReadOnlySpan<char>.Empty;
        ReadOnlySpan<char> minimumVisualStudioVersion = ReadOnlySpan<char>.Empty;
        while ((readLine = reader.ReadLine()) != null)
        {
            ReadOnlySpan<char> line = readLine;
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
            return new VSSolutionFile(new string(minimumVisualStudioVersion), new string(visualStudioVersion));
        }
        throw new InvalidDataException("Missing version info");
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

    public static bool TryGetMinimumVisualStudio(BaseProjectModel project, [NotNullWhen(true)] out string? result)
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
