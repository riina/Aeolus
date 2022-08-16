using cl;
using CrossLaunch;
using CrossLaunch.Unity;

var cfg = new CLConfiguration { Evaluators = new IProjectEvaluator[] { new UnitySupport() } };
var instance = await CLInstance.CreateAsync(cfg);
// TODO commands
await instance.AddDirectoryAsync(Environment.CurrentDirectory);
await instance.UpdateAllDirectoriesAsync();
foreach (var x in instance.Db.ProjectDirectoryProjects) Console.WriteLine($"{x.FullPath} ({instance.GetPlatformName(x)} {instance.GetDisplayFramework(x)})");
