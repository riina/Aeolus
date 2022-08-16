using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Globalization;
using System.Text;
using cl;
using CrossLaunch;
using CrossLaunch.Models;
using CrossLaunch.Unity;


var rootCommand = new RootCommand();
var verboseOption = new Option<bool>("--verbose", "Enable verbose output");
rootCommand.Description = "Launch utility";
// folder
var folderCommand = new Command("folder", "Manage project folders");
rootCommand.Add(folderCommand);
// folder add
var folderAddPathsArgument = new Argument<string[]>("paths", description: "Target paths");
var folderAddCommand = new Command("add", "Add project folders") { folderAddPathsArgument };
folderAddCommand.Handler = CommandHandler.Create(async (string[] paths) =>
{
    var instance = await CLInstance.CreateAsync(GetConfiguration());
    foreach (string path in paths.Select(Path.GetFullPath))
    {
        if (!Directory.Exists(path))
        {
            Console.WriteLine($"Directory \"{path}\" does not exist");
            continue;
        }
        var result = await instance.AddDirectoryAsync(path);
        if (result.Success) await instance.UpdateDirectoryAsync(result.Model);
        Console.WriteLine(result.Success ? $"Directory \"{path}\" added" : $"Directory \"{path}\" already registered");
    }
});
folderCommand.Add(folderAddCommand);
// folder remove
var folderRemovePathsArgument = new Argument<string[]>("paths", description: "Target paths");
var folderRemoveCommand = new Command("remove", "Remove project folders") { folderRemovePathsArgument };
folderRemoveCommand.Handler = CommandHandler.Create(async (string[] paths) =>
{
    var instance = await CLInstance.CreateAsync(GetConfiguration());
    foreach (string path in paths.Select(Path.GetFullPath))
    {
        bool removed = await instance.RemoveDirectoryAsync(path);
        Console.WriteLine(removed ? $"Directory \"{path}\" removed" : $"Directory \"{path}\" not registered");
    }
});
folderCommand.Add(folderRemoveCommand);
// folder list
var folderListTotalCommand = new Option<bool>("--total", "Show total project counts");
var folderListCommand = new Command("list", "List project folders") { folderListTotalCommand };
folderListCommand.Handler = CommandHandler.Create(async (bool total) =>
{
    var instance = await CLInstance.CreateAsync(GetConfiguration());
    foreach (var folder in instance.Db.ProjectDirectories)
    {
        Console.WriteLine(total
            ? $"{folder.FullPath} ({folder.Projects.Count} projects)"
            : folder.FullPath);
    }
});
folderCommand.Add(folderListCommand);
// folder refresh
var folderRefreshPathsArgument = new Argument<string[]>("paths", description: "Target paths");
var folderRefreshCommand = new Command("refresh", "Refresh projects") { verboseOption, folderRefreshPathsArgument };
folderRefreshCommand.Handler = CommandHandler.Create(async (bool verbose, string[] paths) =>
{
    var instance = await CLInstance.CreateAsync(GetConfiguration());
    if (paths.Length == 0)
    {
        await instance.UpdateAllDirectoriesAsync();
    }
    else
        foreach (string path in paths.Select(Path.GetFullPath))
        {
            var dir = await instance.Db.ProjectDirectories.FindAsync(path);
            if (dir != null)
            {
                await instance.UpdateDirectoryAsync(dir);
            }
            else
            {
                if (verbose) Console.WriteLine($"Directory \"{path}\" not registered");
            }
        }
});
folderCommand.Add(folderRefreshCommand);
// project
var projectCommand = new Command("project", "Manage projects");
rootCommand.Add(projectCommand);
// project list
var projectListCommand = new Command("list", "List projects");
projectListCommand.Handler = CommandHandler.Create(async () =>
{
    var instance = await CLInstance.CreateAsync(GetConfiguration());
    foreach (var project in instance.Db.ProjectDirectoryProjects)
    {
        StringBuilder sb = new();
        if (project.Nickname is { } nick) sb.Append(nick).Append(" - ");
        sb.Append(CultureInfo.InvariantCulture, $"{project.FullPath} ({instance.GetPlatformName(project)} {instance.GetDisplayFramework(project)})");
        Console.WriteLine(sb.ToString());
    }
});
projectCommand.Add(projectListCommand);
// project recent
var projectRecentCommand = new Command("recent", "List recent projects");
projectRecentCommand.Handler = CommandHandler.Create(async () =>
{
    var instance = await CLInstance.CreateAsync(GetConfiguration());
    foreach (var project in instance.Db.RecentProjects.OrderByDescending(v => v.OpenedTime))
    {
        StringBuilder sb = new();
        if (project.Nickname is { } nick) sb.Append(nick).Append(" - ");
        sb.Append(CultureInfo.InvariantCulture, $"{project.FullPath} ({instance.GetPlatformName(project)} {instance.GetDisplayFramework(project)})");
        Console.WriteLine(sb.ToString());
    }
});
projectCommand.Add(projectRecentCommand);
// project launch
var projectLaunchProjectArgument = new Argument<string>("project", description: "Target project path or nick");
var projectLaunchInteractiveOption = new Option<bool>("--interactive", "Allow interactive remediations");
var projectLaunchCommand = new Command("launch", "Launch project") { projectLaunchProjectArgument, projectLaunchInteractiveOption };
var xCommand = new Command("x", "Launch project") { projectLaunchProjectArgument, projectLaunchInteractiveOption };
var projectLaunchCommandHandler = CommandHandler.Create(async (string project, bool interactive) =>
{
    var instance = await CLInstance.CreateAsync(GetConfiguration());
    BaseProjectModel? toLaunch;
    string fullPath = Path.GetFullPath(project);
    toLaunch = instance.Db.ProjectDirectoryProjects.FirstOrDefault(v => v.Nickname == project);
    toLaunch ??= await instance.Db.ProjectDirectoryProjects.FindAsync(fullPath);
    if (toLaunch == null)
    {
        Console.WriteLine($"Project with path or nickname \"{project}\" not found");
        return 1;
    }
    var result = await instance.LoadAsync(toLaunch);
    if (result.Success)
    {
        await instance.PushRecentProjectAsync(toLaunch);
        return 0;
    }
    else
    {
        if (result.FailInfo is { } failInfo)
        {
            Console.WriteLine();
            Console.WriteLine($"## {failInfo.Title} ##");
            Console.WriteLine();
            Console.WriteLine(failInfo.ErrorMessage);
            if (failInfo.Remediations.Length != 0)
            {
                Console.WriteLine();
                Console.WriteLine("## Options ##");
                Console.WriteLine();
                if (interactive)
                {
                    Console.WriteLine("0: Quit");
                    Console.WriteLine();
                }
                for (int i = 0; i < failInfo.Remediations.Length; i++)
                {
                    ProjectLoadFailRemediation remediation = failInfo.Remediations[i];
                    Console.WriteLine(interactive ? $"{i + 1}: {remediation.ActionShortName}" : $"-- {remediation.ActionShortName}");
                    Console.WriteLine(remediation.ActionDescription);
                    Console.WriteLine();
                }
                if (interactive)
                {
                    while (true)
                    {
                        Console.Write("Select an option: ");
                        if (!int.TryParse(Console.ReadLine(), out int choice))
                        {
                            Console.WriteLine("Invalid input: enter a number");
                        }
                        else if (choice == 0)
                        {
                            return 0;
                        }
                        else
                        {
                            choice--;
                            if (choice >= failInfo.Remediations.Length)
                            {
                                Console.WriteLine("Invalid input: out of range");
                            }
                            else
                            {
                                await failInfo.Remediations[choice].Callback();
                                return 0;
                            }
                        }
                    }
                }
            }
        }
        else
        {
            Console.WriteLine($"Project \"{project}\" failed to open");
        }
        return 2;
    }
});
projectLaunchCommand.Handler = projectLaunchCommandHandler;
xCommand.Handler = projectLaunchCommandHandler;
projectCommand.Add(projectLaunchCommand);
rootCommand.Add(xCommand);
// project nick
var projectNickProjectArgument = new Argument<string>("project", description: "Target project path");
var projectNickNickArgument = new Argument<string>("nick", description: "Nickname to set");
var projectNickCommand = new Command("nick", "Set project nickname") { projectNickProjectArgument, projectNickNickArgument };
projectNickCommand.Handler = CommandHandler.Create(async (string project, string nick) =>
{
    var instance = await CLInstance.CreateAsync(GetConfiguration());
    BaseProjectModel? toNick = await instance.Db.ProjectDirectoryProjects.FindAsync(Path.GetFullPath(project));
    if (toNick != null)
    {
        toNick.Nickname = nick;
        await instance.Db.SaveChangesAsync();
    }
});
projectCommand.Add(projectNickCommand);
// project unnick
var projectUnnickProjectArgument = new Argument<string>("project", description: "Target project path");
var projectUnnickCommand = new Command("unnick", "Unset project nickname") { projectUnnickProjectArgument };
projectUnnickCommand.Handler = CommandHandler.Create(async (string project) =>
{
    var instance = await CLInstance.CreateAsync(GetConfiguration());
    BaseProjectModel? toNick = await instance.Db.ProjectDirectoryProjects.FindAsync(Path.GetFullPath(project));
    if (toNick != null)
    {
        toNick.Nickname = null;
        await instance.Db.SaveChangesAsync();
    }
});
projectCommand.Add(projectUnnickCommand);
return await rootCommand.InvokeAsync(args);

static CLConfiguration GetConfiguration() => new() { Evaluators = new IProjectEvaluator[] { new UnitySupport() }, MaxRecentProjects = 10, MaxDepth = 2 };
