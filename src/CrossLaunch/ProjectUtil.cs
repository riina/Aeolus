using System.Text.RegularExpressions;
using CrossLaunch.Models;
using Microsoft.EntityFrameworkCore;

namespace CrossLaunch;

public static class ProjectUtil
{
    public static async Task AddProjectDirectoryAsync(this CLContextBase context, ProjectDirectoryModel projectDirectory)
    {
        context.ProjectDirectories.Add(projectDirectory);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public static async Task RemoveProjectDirectoryAsync(this CLContextBase context, ProjectDirectoryModel projectDirectory)
    {
        context.ProjectDirectories.Remove(projectDirectory);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public static async Task PushRecentProjectAsync(this CLContextBase context, CLConfiguration configuration, RecentProjectModel recentProject)
    {
        DateTime dateTime = DateTime.Now;
        recentProject.OpenedTime = dateTime;
        recentProject.RecordUpdateTime = dateTime;
        context.RecentProjects.Add(recentProject);
        await context.SaveChangesAsync().ConfigureAwait(false);
        int existingCount = await context.RecentProjects.CountAsync();
        int over = existingCount - Math.Max(0, configuration.MaxRecentProjects);
        if (over > 0) context.RecentProjects.RemoveRange(await context.RecentProjects.OrderBy(v => v.OpenedTime).Take(over).ToListAsync().ConfigureAwait(false));
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public static async Task PushRecentProjectAsync(this CLContextBase context, CLConfiguration configuration, ProjectDirectoryProjectModel projectDirectoryProject)
    {
        var recentProject = await context.RecentProjects.FindAsync(projectDirectoryProject.FullPath) ?? new RecentProjectModel { FullPath = projectDirectoryProject.FullPath, Framework = projectDirectoryProject.Framework, RecordUpdateTime = DateTime.Now, ProjectEvaluatorType = projectDirectoryProject.ProjectEvaluatorType };
        await PushRecentProjectAsync(context, configuration, recentProject);
    }

    public static async Task UpdateProjectDirectoryProjectListAsync(this CLContextBase context, ProjectDirectoryModel directory, IReadOnlyList<IProjectEvaluator> evaluators)
    {
        string projectDirectory = directory.FullPath;
        DateTime recordUpdateTime = DateTime.Now;
        try
        {
            foreach (var evaluator in evaluators)
            {
                string evaluatorType = CreateToolString(evaluator.GetType());
                await foreach (var x in evaluator.FindProjectsAsync(projectDirectory).ConfigureAwait(false))
                {
                    ProjectDirectoryProjectModel project = await context.ProjectDirectoryProjects.FindAsync(x.FullPath) ?? new ProjectDirectoryProjectModel { FullPath = x.FullPath, };
                    project.ProjectDirectory = directory;
                    project.ProjectEvaluatorType = evaluatorType;
                    project.Framework = x.Framework;
                    project.RecordUpdateTime = recordUpdateTime;
                    context.ProjectDirectoryProjects.Add(project);
                }
            }
        }
        catch
        {
            // ignored
        }
        await context.SaveChangesAsync().ConfigureAwait(false);
        context.ProjectDirectoryProjects.RemoveRange(context.ProjectDirectoryProjects.Where(v => v.RecordUpdateTime != recordUpdateTime));
        await context.SaveChangesAsync().ConfigureAwait(false);
    }


    private static string CreateToolString(Type type)
    {
        string assemblyName = type.Assembly.GetName().Name ?? throw new InvalidOperationException();
        string typeName = type.FullName ?? throw new InvalidOperationException();
        return $"{assemblyName}::{typeName}";
    }

    private static (string assembly, string type) GetId(string tool)
    {
        if (tool == null) throw new ArgumentNullException(nameof(tool));
        if (s_toolRegex.Match(tool) is not { Success: true } match)
            throw new ArgumentException("Tool string is in invalid format, must be \"<assembly>::<toolType>\"", nameof(tool));
        return (match.Groups[1].Value, match.Groups[2].Value);
    }

    private static readonly Regex s_toolRegex = new(@"^([\S\s]+)::([\S\s]+)$");
}
