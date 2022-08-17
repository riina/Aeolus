using Aeolus.ModelProxies;
using CrossLaunch;

namespace Aeolus;

internal static class Extensions
{
    public static List<ProjectDirectoryProject> GetProjects(this CLInstance cl)
        => cl.Db.ProjectDirectoryProjects.Select(v => new ProjectDirectoryProject
        {
            Name = Path.GetFileName(v.FullPath),
            FullPath = v.FullPath,
            SoftwareAndFramework = $"{cl.GetPlatformName(v)} {cl.GetDisplayFramework(v)}"
        }).ToList();
}
