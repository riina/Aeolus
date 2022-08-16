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
    private static readonly HashSet<Guid> s_riderTypes = new HashSet<Guid>() { Guid.ParseExact("9a19103f-16f7-4668-be54-9a1e7a4f7556", "D"), Guid.ParseExact("fae04ec0-301f-11d3-bf4b-00c04f79efbc", "D") };

    public Task<ProjectLoadResult> TryLoadAsync(BaseProjectModel project, CLConfiguration configuration)
    {
        if (!VSSolutionFile.TryGetMinimumVisualStudio(project, out string? framework)) return Task.FromResult(ProjectLoadResult.BadFrameworkId(project.Framework));
        VSSolutionFile solutionFile;
        try
        {
            using var stream = File.OpenText(project.FullPath);
            solutionFile = VSSolutionFile.Load(stream);
        }
        catch (InvalidDataException)
        {
            return Task.FromResult(ProjectLoadResult.InvalidFile);
        }
        var remediations = new List<ProjectLoadFailRemediation>();
        if (configuration.TryGetFlag("visualstudio.rider.enable", out _))
        {
            if (solutionFile.ProjectDefinitions.Any(v => s_riderTypes.Contains(v.Kind)))
            {
                string? exePath = null;
                if (OperatingSystem.IsMacOS())
                {
                    string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify), "Library/Application Support/JetBrains/Toolbox/apps/Rider/ch-0");
                    if (Directory.Exists(baseDir) && GetMaxRiderFolder(baseDir) is { } riderSub)
                        exePath = FSUtil.IfFileExists(Path.Combine(baseDir, riderSub, "Rider.app/Contents/MacOS/rider"));
                }
                else if (OperatingSystem.IsWindows())
                {
                    string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify), @"JetBrains\Toolbox\apps\Rider\ch-0");
                    if (Directory.Exists(baseDir) && GetMaxRiderFolder(baseDir) is { } riderSub)
                        exePath = FSUtil.IfFileExists(Path.Combine(baseDir, riderSub, @"bin\rider64.exe"));
                }
                if (exePath != null)
                {
                    ProcessUtils.Start(exePath, Path.GetDirectoryName(project.FullPath) ?? "");
                    return Task.FromResult(ProjectLoadResult.Successful);
                }
                remediations.Add(new ProjectLoadFailRemediation("Get JetBrains Rider", @"Install JetBrains Rider, a feature-rich proprietary IDE primarily for .NET development.
https://www.jetbrains.com/rider/", ProcessUtils.GetUriCallback("https://www.jetbrains.com/rider/")));
            }
        }
        if (configuration.TryGetFlag("visualstudio.vscode.enable", out _))
        {
            string? exePath = null;
            if (OperatingSystem.IsMacOS())
                exePath = FSUtil.IfFileExists("/Applications/Visual Studio Code.app/Contents/MacOS/Electron");
            else if (OperatingSystem.IsWindows())
                exePath = FSUtil.IfFileExists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft VS Code", "Code.exe"));
            if (exePath != null)
            {
                ProcessUtils.Start(exePath, Path.GetDirectoryName(project.FullPath) ?? "");
                return Task.FromResult(ProjectLoadResult.Successful);
            }
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

    private static string? GetMaxRiderFolder(string dir) =>
        Directory.GetDirectories(dir)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Select(v => CLSemVer.TryParse(v, out var ver) ? new CLSemVerTag(v, ver) : null)
            .OfType<CLSemVerTag>()
            .MaxBy(v => v.Version)?.Key;
}

public class VSSolutionFile
{
    public readonly string MinimumVisualStudioVersion;
    public readonly string VisualStudioVersion;
    public IReadOnlyList<ProjectDefinition> ProjectDefinitions;

    public VSSolutionFile(string minimumVisualStudioVersion, string visualStudioVersion, IReadOnlyList<ProjectDefinition> projectDefinitions)
    {
        MinimumVisualStudioVersion = minimumVisualStudioVersion;
        VisualStudioVersion = visualStudioVersion;
        ProjectDefinitions = projectDefinitions;
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
        if (visualStudioVersion.Length == 0 || minimumVisualStudioVersion.Length == 0) throw new InvalidDataException("Missing version info");
        ParseState state = new(new List<ProjectDefinition>());
        ProcessSection(ReadOnlySpan<char>.Empty, ref state, reader);
        return new VSSolutionFile(new string(minimumVisualStudioVersion), new string(visualStudioVersion), state.ProjectDefinitions);
    }

    private static void ProcessSection(ReadOnlySpan<char> section, ref ParseState state, TextReader reader)
    {
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            ReadOnlySpan<char> l = line.TrimStart();
            if (section.Length != 0 && l.StartsWith("End") && l[3..].TrimEnd().SequenceEqual(section))
            {
                return;
            }
            ReadOnlySpan<char> key;
            ReadOnlySpan<char> name = ReadOnlySpan<char>.Empty;
            ReadOnlySpan<char> value;
            if (TryGetKeyValue(l, out var k1, out var v1))
            {
                key = k1;
                value = v1;
                if (TryGetNamedKey(key, out var k2, out var n1))
                {
                    key = k2;
                    name = n1;
                }
            }
            else
            {
                key = l;
                value = ReadOnlySpan<char>.Empty;
            }
            if (key.SequenceEqual("Project"))
            {
                ProcessSection("Project", ref state, reader);
                var nameEntries = ParseEntries(name);
                var valueEntries = ParseEntries(value);
                if (nameEntries.Count != 1) throw new InvalidDataException("Unexpected number of entries for Project element kind");
                if (!Guid.TryParseExact(nameEntries[0], "B", out var nameGuid)) throw new InvalidDataException("Invalid GUID for Project element kind");
                if (valueEntries.Count != 3) throw new InvalidDataException("Unexpected number of entries for Project element data");
                if (!Guid.TryParseExact(valueEntries[2], "B", out var projectGuid)) throw new InvalidDataException("Invalid GUID for Project");
                state.ProjectDefinitions.Add(new ProjectDefinition(nameGuid, valueEntries[0], valueEntries[1], projectGuid));
            }
            else if (key.SequenceEqual("Global"))
            {
                ProcessSection("Global", ref state, reader);
            }
            else if (key.SequenceEqual("GlobalSection"))
            {
                ProcessSection("GlobalSection", ref state, reader);
            }
        }
        if (section.Length != 0) throw new InvalidDataException("Unexpected EOF");
    }

    private struct ParseState
    {
        public List<ProjectDefinition> ProjectDefinitions;

        public ParseState(List<ProjectDefinition> projectDefinitions)
        {
            ProjectDefinitions = projectDefinitions;
        }
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

    private static bool TryGetKeyValue(ReadOnlySpan<char> source, out ReadOnlySpan<char> key, out ReadOnlySpan<char> value)
    {
        int index = source.IndexOf('=');
        if (index != -1)
        {
            key = source[..index].Trim();
            value = source[(index + 1)..].Trim();
            return true;
        }
        key = ReadOnlySpan<char>.Empty;
        value = ReadOnlySpan<char>.Empty;
        return false;
    }

    private static bool TryGetNamedKey(ReadOnlySpan<char> key, out ReadOnlySpan<char> keyType, out ReadOnlySpan<char> keyName)
    {
        int index = key.IndexOf('(');
        if (index != -1 && key[^1] == ')')
        {
            keyType = key[..index];
            keyName = key[(index + 1)..^1];
            return true;
        }
        keyType = ReadOnlySpan<char>.Empty;
        keyName = ReadOnlySpan<char>.Empty;
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

    private static List<string> ParseEntries(ReadOnlySpan<char> buf)
    {
        var res = new List<string>();
        buf = buf.Trim();
        while (!buf.IsEmpty)
        {
            if (buf[0] != '"') throw new InvalidDataException("Expected '\"'");
            buf = buf[1..];
            int index2 = buf.IndexOf('"');
            if (index2 == -1) throw new InvalidDataException("Expected '\"'");
            ReadOnlySpan<char> sub = buf[..index2];
            buf = buf[(index2 + 1)..].TrimStart();
            if (buf.StartsWith(",")) buf = buf[1..].TrimStart();
            else if (!buf.IsEmpty) throw new InvalidDataException("Expected ''");
            res.Add(new string(sub));
        }
        return res;
    }

    public record ProjectDefinition(Guid Kind, string Directory, string File, Guid Guid);
}
