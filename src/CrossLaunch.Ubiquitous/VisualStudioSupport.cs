using System.Text;
using CrossLaunch.Models;
using CrossLaunch.Ubiquitous.Formats;

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
