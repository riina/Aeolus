using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using CrossLaunch.Models;
using CrossLaunch.Ubiquitous.Formats;

namespace CrossLaunch.Ubiquitous.Projects;

public record VisualStudioSolution(string FullPath, VisualStudioSolutionFile SolutionFile) : ProjectBase(FullPath)
{
    public override string FrameworkString => $"{SolutionFile.MinimumVisualStudioVersion}/{SolutionFile.VisualStudioVersion}";

    public static async Task<ProjectParseResult<VisualStudioSolution>> ParseAsync(string path)
    {
        if (!".sln".Equals(Path.GetExtension(path), StringComparison.InvariantCultureIgnoreCase)) return ProjectParseResult<VisualStudioSolution>.InvalidExtension;
        try
        {
            using var stream = File.OpenText(path);
            var solution = await VisualStudioSolutionFile.LoadAsync(stream);
            var sb = new StringBuilder();
            sb.Append(solution.MinimumVisualStudioVersion).Append('/').Append(solution.VisualStudioVersion);
            return new ProjectParseResult<VisualStudioSolution>(new VisualStudioSolution(Path.GetFullPath(path), solution));
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
            // Take origin version (not minimum) for display
            slice = slice[(index + 1)..];
            int index2 = slice.IndexOf('.');
            if (index2 != -1)
            {
                if (int.TryParse(slice[..index2], out int verInt))
                {
                    result = s_versionMap.TryGetValue(verInt, out string? r) ? r : $"v{verInt}";
                    return true;
                }
            }
        }
        result = null;
        return false;
    }

    private static readonly Dictionary<int, string> s_versionMap = new()
    {
        { 5, "97" },
        { 6, "6.0" },
        { 7, ".NET" },
        { 8, "2005" },
        { 9, "2008" },
        { 10, "2010" },
        { 11, "2012" },
        { 12, "2013" },
        { 14, "2015" },
        { 15, "2017" },
        { 16, "2019" },
        { 17, "2022" }
    };

    private static readonly HashSet<Guid> s_riderTypes = new() { Guid.ParseExact("9a19103f-16f7-4668-be54-9a1e7a4f7556", "D"), Guid.ParseExact("fae04ec0-301f-11d3-bf4b-00c04f79efbc", "D") };

    public override async Task<ProjectLoadResult> TryLoadAsync(CLConfiguration configuration)
    {
        string projectDir = Path.GetDirectoryName(FullPath) ?? "";
        var remediations = new List<ProjectLoadFailRemediation>();
        if (configuration.TryGetFlag("visualstudio.rider.enable", out _))
        {
            if (SolutionFile.ProjectDefinitions.Any(v => s_riderTypes.Contains(v.Kind)))
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
                    ProcessUtils.Start(exePath, FullPath);
                    return ProjectLoadResult.Successful;
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
                ProcessUtils.Start(exePath, projectDir);
                return ProjectLoadResult.Successful;
            }
            remediations.Add(new ProjectLoadFailRemediation("Get Visual Studio Code", @"Install Visual Studio Code from Microsoft Corporation, a lightweight proprietary code editor.
https://code.visualstudio.com/", ProcessUtils.GetUriCallback("https://code.visualstudio.com/")));
        }
        if (OperatingSystem.IsWindows())
        {
            string vswhere = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolderOption.DoNotVerify), @"Microsoft Visual Studio\Installer\vswhere.exe");
            if (File.Exists(vswhere))
            {
                var proc = new Process();
                proc.StartInfo.FileName = vswhere;
                proc.StartInfo.Arguments = "-latest -format value -property productPath";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                string? line = null;
                proc.OutputDataReceived += (_, args) => line ??= args.Data;
                proc.Start();
                proc.BeginOutputReadLine();
                await proc.WaitForExitAsync();
                if (line != null)
                {
                    ProcessUtils.Start(line, FullPath);
                    return ProjectLoadResult.Successful;
                }
            }
            remediations.Add(new ProjectLoadFailRemediation("Get Visual Studio", @"Install Visual Studio from Microsoft Corporation, a feature-rich proprietary IDE.
https://visualstudio.microsoft.com/vs/", ProcessUtils.GetUriCallback("https://visualstudio.microsoft.com/vs/")));
        }
        else if (OperatingSystem.IsMacOS())
        {
            string? exePath = FSUtil.IfFileExists("/Applications/Visual Studio.app/Contents/MacOS/VisualStudio");
            if (exePath != null)
            {
                ProcessUtils.Start(exePath, FullPath);
                return ProjectLoadResult.Successful;
            }
            remediations.Add(new ProjectLoadFailRemediation("Get Visual Studio for Mac", @"Install Visual Studio for Mac from Microsoft Corporation, a proprietary IDE primarily for .NET and Xamarin development.
https://visualstudio.microsoft.com/vs/mac/", ProcessUtils.GetUriCallback("https://visualstudio.microsoft.com/vs/mac/")));
        }
        return ProjectLoadResult.Failure("No Valid Program Found", @"Failed to identify software capable of opening this Visual Studio solution file.

A program such as Visual Studio or Rider must be installed.", remediations.ToArray());
    }

    private static string? GetMaxRiderFolder(string dir) =>
        Directory.GetDirectories(dir)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Select(v => CLSemVer.TryParse(v, out var ver) ? new CLSemVerTag(v, ver) : null)
            .OfType<CLSemVerTag>()
            .MaxBy(v => v.Version)?.Key;
}
