using cl;
using CrossLaunch;
using CrossLaunch.Models;
using CrossLaunch.Unity;
using Microsoft.EntityFrameworkCore;

var evaluators = new IProjectEvaluator[] { new UnitySupport() };
var typeMap = evaluators.ToDictionary(v => v.GetType());
var dbFac = new CLContextFactory();
var db = dbFac.CreateDbContext(Array.Empty<string>());
await db.Database.MigrateAsync();
var cfg = new CLConfiguration();
// TODO commands
await db.AddProjectDirectoryAsync(new ProjectDirectoryModel { FullPath = Environment.CurrentDirectory, Projects = new HashSet<ProjectDirectoryProjectModel>(), RecordUpdateTime = DateTime.Now });
foreach (var dir in db.ProjectDirectories.ToList())
    await db.UpdateProjectDirectoryProjectListAsync(dir, cfg, evaluators);
foreach (var x in db.ProjectDirectoryProjects)
{
    var typeString = TypeTool.ParseTypeString(x.ProjectEvaluatorType);
    var type = typeString.Load();
    string platformName = typeMap.TryGetValue(type, out var evaluator) ? evaluator.FriendlyPlatformName : "unknown";
    Console.WriteLine($"{x.FullPath} ({platformName} {x.Framework})");
}
