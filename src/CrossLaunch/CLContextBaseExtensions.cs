using CrossLaunch.Models;
using Microsoft.EntityFrameworkCore;

namespace CrossLaunch;

public static class CLContextBaseExtensions
{
    public static async Task<ProjectDirectoryModel> AddProjectDirectoryAsync(this CLContextBase context, ProjectDirectoryModel projectDirectory)
    {
        DateTime now = DateTime.Now;
        ProjectDirectoryModel result;
        if (await context.ProjectDirectories.FindAsync(projectDirectory.FullPath).ConfigureAwait(false) is { } existing)
        {
            result = existing;
            context.ProjectDirectories.Update(existing);
        }
        else
        {
            result = projectDirectory;
            context.ProjectDirectories.Add(projectDirectory);
        }
        result.RecordUpdateTime = now;
        await context.SaveChangesAsync().ConfigureAwait(false);
        return result;
    }

    public static async Task<bool> RemoveProjectDirectoryAsync(this CLContextBase context, ProjectDirectoryModel projectDirectory)
    {
        if (await context.ProjectDirectories.FindAsync(projectDirectory.FullPath).ConfigureAwait(false) is { } existing)
        {
            context.ProjectDirectories.Remove(existing);
            await context.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }
        return false;
    }

    public static async Task<RecentProjectModel> PushRecentProjectAsync(this CLContextBase context, CLConfiguration configuration, RecentProjectModel recentProject)
    {
        DateTime dateTime = DateTime.Now;
        //if(await context.RecentProjects.FindAsync(recentProject.FullPath))
        RecentProjectModel result;
        if (await context.RecentProjects.FindAsync(recentProject.FullPath).ConfigureAwait(false) is { } existing)
        {
            existing.ProjectEvaluatorType = recentProject.ProjectEvaluatorType;
            existing.Framework = recentProject.Framework;
            existing.Nickname = recentProject.Nickname;
            result = existing;
            context.RecentProjects.Update(existing);
        }
        else
        {
            result = recentProject;
            context.RecentProjects.Add(recentProject);
        }
        result.OpenedTime = dateTime;
        result.RecordUpdateTime = dateTime;
        await context.SaveChangesAsync().ConfigureAwait(false);
        int existingCount = await context.RecentProjects.CountAsync().ConfigureAwait(false);
        int over = existingCount - Math.Max(0, configuration.MaxRecentProjects);
        if (over > 0) context.RecentProjects.RemoveRange(await context.RecentProjects.OrderBy(v => v.OpenedTime).Take(over).ToListAsync().ConfigureAwait(false));
        await context.SaveChangesAsync().ConfigureAwait(false);
        return result;
    }

    public static async Task PushRecentProjectAsync(this CLContextBase context, CLConfiguration configuration, ProjectDirectoryProjectModel projectDirectoryProject)
    {
        var recentProject = await context.RecentProjects.FindAsync(projectDirectoryProject.FullPath).ConfigureAwait(false) ?? new RecentProjectModel { FullPath = projectDirectoryProject.FullPath, Framework = projectDirectoryProject.Framework, RecordUpdateTime = DateTime.Now, ProjectEvaluatorType = projectDirectoryProject.ProjectEvaluatorType };
        await PushRecentProjectAsync(context, configuration, recentProject).ConfigureAwait(false);
    }

    public static async Task UpdateProjectDirectoryProjectListAsync(this CLContextBase context, ProjectDirectoryModel directory, CLConfiguration configuration, IReadOnlyCollection<IProjectEvaluator> evaluators)
    {
        string projectDirectory = directory.FullPath;
        DateTime recordUpdateTime = DateTime.Now;
        try
        {
            foreach (var evaluator in evaluators)
            {
                string evaluatorType = TypeTool.CreateTypeString(evaluator.GetType());
                await foreach (var x in evaluator.FindProjectsAsync(projectDirectory, configuration).ConfigureAwait(false))
                {
                    ProjectDirectoryProjectModel? project = await context.ProjectDirectoryProjects.FindAsync(x.FullPath).ConfigureAwait(false);
                    bool created;
                    if (project == null)
                    {
                        project = new ProjectDirectoryProjectModel { FullPath = x.FullPath };
                        created = true;
                    }
                    else created = false;
                    project.ProjectDirectory = directory;
                    project.ProjectEvaluatorType = evaluatorType;
                    project.Framework = x.Framework;
                    project.RecordUpdateTime = recordUpdateTime;
                    if (created) context.ProjectDirectoryProjects.Add(project);
                    else context.ProjectDirectoryProjects.Update(project);
                }
            }
        }
        catch
        {
            // ignored
        }
        await context.SaveChangesAsync().ConfigureAwait(false);
        string key = directory.FullPath;
        context.ProjectDirectoryProjects.RemoveRange(context.ProjectDirectoryProjects.Where(v => v.ProjectDirectory.FullPath == key && v.RecordUpdateTime != recordUpdateTime));
        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}
