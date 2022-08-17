using Aeolus.ModelProxies;
using CrossLaunch;

namespace Aeolus;

internal static class Extensions
{
    public static List<ProjectDirectory> GetProjectDirectories(this CLInstance cl)
        => cl.Db.ProjectDirectories.Select(v => new ProjectDirectory
        {
            FullPath = v.FullPath
        }).ToList();

    public static List<ProjectDirectoryProject> GetProjectDirectoryProjects(this CLInstance cl)
        => cl.Db.ProjectDirectoryProjects.Select(v => new ProjectDirectoryProject
        {
            Name = Path.GetFileName(v.FullPath),
            FullPath = v.FullPath,
            SoftwareAndFramework = $"{cl.GetPlatformName(v)}\n{cl.GetDisplayFramework(v)}"
        }).ToList();

    public static List<Remediation> GetRemediations(this ProjectLoadFailInfo failInfo)
        => failInfo.Remediations.Select(v => new Remediation
        {
            ActionShortName = v.ActionShortName,
            ActionDescription = v.ActionDescription,
            Callback = v.Callback
        }).ToList();
}
