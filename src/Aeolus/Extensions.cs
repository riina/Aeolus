using Aeolus.ModelProxies;
using CrossLaunch;
using Microsoft.EntityFrameworkCore;

namespace Aeolus;

internal static class Extensions
{
    public static List<ProjectDirectory> GetProjectDirectories(this CLInstance cl, Style[] styleCycle)
        => cl.Db.ProjectDirectories.ToList().Select((v, i) => new ProjectDirectory(
            FullPath: v.FullPath,
            Style: styleCycle[i % styleCycle.Length]
        )).ToList();

    public static List<RecentProject> GetRecentProjects(this CLInstance cl, Style[] styleCycle)
        => cl.Db.RecentProjects.OrderByDescending(v => v.OpenedTime).ToList().Select((v, i) => new RecentProject(
            Name: Path.GetFileName(v.FullPath),
            FullPath: v.FullPath,
            SoftwareAndFramework: $"{cl.GetPlatformName(v)}\n{cl.GetDisplayFramework(v)}",
            Style: styleCycle[i % styleCycle.Length]
        )).ToList();

    public static List<ProjectDirectoryProject> GetProjectDirectoryProjects(this CLInstance cl, Style[] styleCycle)
        => cl.Db.ProjectDirectoryProjects.ToList().Select((v, i) => new ProjectDirectoryProject(
            Name: Path.GetFileName(v.FullPath),
            FullPath: v.FullPath,
            SoftwareAndFramework: $"{cl.GetPlatformName(v)}\n{cl.GetDisplayFramework(v)}",
            Style: styleCycle[i % styleCycle.Length])
        ).ToList();

    public static List<ProjectDirectoryProject> Filter(this List<ProjectDirectoryProject> list, string search, Style[] styleCycle)
        => string.IsNullOrEmpty(search)
        ? list
        : list.Where(v => v.FullPath.Contains(search, StringComparison.InvariantCultureIgnoreCase))
        .Select((v, i) => v with { Style = styleCycle[i % styleCycle.Length] })
        .ToList();

    public static List<Remediation> GetRemediations(this ProjectLoadFailInfo failInfo)
        => failInfo.Remediations.ToList().Select(v => new Remediation(
            ActionShortName: v.ActionShortName,
            ActionDescription: v.ActionDescription,
            Callback: v.Callback
        )).ToList();
}
