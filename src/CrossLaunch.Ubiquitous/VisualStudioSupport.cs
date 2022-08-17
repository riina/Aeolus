using CrossLaunch.Models;
using CrossLaunch.Ubiquitous.Formats;
using CrossLaunch.Ubiquitous.Projects;

namespace CrossLaunch.Ubiquitous;

public class VisualStudioSupport : FileSupportBase<VisualStudioProjectLoader>
{
    public override string FriendlyPlatformName => "Visual Studio";

    public override async Task<EvaluatedProject?> EvaluateProjectAsync(string path, CLConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var loadResult = await VisualStudioSolution.LoadAsync(path);
        return loadResult.Result is { } result ? new EvaluatedProject(Path.GetFullPath(path), result.FrameworkString) : null;
    }

    public override string GetDisplayFramework(BaseProjectModel project)
        => VisualStudioSolution.TryGetDisplayFramework(project, out string? result) ? result : project.Framework;
}

public class VisualStudioProjectLoader : IProjectLoader
{
    private static readonly HashSet<Guid> s_riderTypes = new() { Guid.ParseExact("9a19103f-16f7-4668-be54-9a1e7a4f7556", "D"), Guid.ParseExact("fae04ec0-301f-11d3-bf4b-00c04f79efbc", "D") };

    public async Task<ProjectLoadResult> TryLoadAsync(BaseProjectModel project, CLConfiguration configuration)
    {
        var loadResult = await VisualStudioSolution.LoadAsync(project.FullPath);
        if (loadResult.Result is not { } result) return loadResult.FailInfo?.AsProjectLoadResult() ?? ProjectLoadResult.Unknown;
        VisualStudioSolutionFile solutionFile = result.SolutionFile;
        string projectDir = Path.GetDirectoryName(project.FullPath) ?? "";
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
                    ProcessUtils.Start(exePath, project.FullPath);
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
            // TODO load through visual studio (e.g. vswhere)
            remediations.Add(new ProjectLoadFailRemediation("Get Visual Studio", @"Install Visual Studio from Microsoft Corporation, a feature-rich proprietary IDE.
https://visualstudio.microsoft.com/vs/", ProcessUtils.GetUriCallback("https://visualstudio.microsoft.com/vs/")));
        }
        else if (OperatingSystem.IsMacOS())
        {
            string? exePath = FSUtil.IfFileExists("/Applications/Visual Studio.app/Contents/MacOS/VisualStudio");
            if (exePath != null)
            {
                ProcessUtils.Start(exePath, projectDir);
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
