using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Globalization;
using System.Text;
using System.Text.Json;
using cl;
using CrossLaunch;
using CrossLaunch.Models;
using CrossLaunch.Ubiquitous;

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
    var instance = await CLInstance.CreateAsync(await GetConfigurationAsync());
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
// folder list
var folderListTotalCommand = new Option<bool>("--total", "Show total project counts");
var folderListCommand = new Command("list", "List project folders") { folderListTotalCommand };
folderListCommand.Handler = CommandHandler.Create(async (bool total) =>
{
    var instance = await CLInstance.CreateAsync(await GetConfigurationAsync());
    foreach (var folder in instance.Db.ProjectDirectories)
    {
        Console.WriteLine(total
            ? $"{folder.FullPath} ({folder.Projects.Count} projects)"
            : folder.FullPath);
    }
});
folderCommand.Add(folderListCommand);
// folder remove
var folderRemovePathsArgument = new Argument<string[]>("paths", description: "Target paths");
var folderRemoveCommand = new Command("remove", "Remove project folders") { folderRemovePathsArgument };
folderRemoveCommand.Handler = CommandHandler.Create(async (string[] paths) =>
{
    var instance = await CLInstance.CreateAsync(await GetConfigurationAsync());
    foreach (string path in paths.Select(Path.GetFullPath))
    {
        bool removed = await instance.RemoveDirectoryAsync(path);
        Console.WriteLine(removed ? $"Directory \"{path}\" removed" : $"Directory \"{path}\" not registered");
    }
});
folderCommand.Add(folderRemoveCommand);
// folder clear
var folderClearCommand = new Command("clear", "Clear project folders");
folderClearCommand.Handler = CommandHandler.Create(async () =>
{
    var instance = await CLInstance.CreateAsync(await GetConfigurationAsync());
    instance.Db.ProjectDirectories.RemoveRange(instance.Db.ProjectDirectories);
    await instance.Db.SaveChangesAsync();
});
folderCommand.Add(folderClearCommand);
// folder scan
var folderScanPathsArgument = new Argument<string[]>("paths", description: "Target paths");
var folderScanCommand = new Command("scan", "(re)scan project folder for projects") { verboseOption, folderScanPathsArgument };
var folderScanCommandHandler = CommandHandler.Create(async (bool verbose, string[] paths) =>
{
    var instance = await CLInstance.CreateAsync(await GetConfigurationAsync());
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
folderScanCommand.Handler = folderScanCommandHandler;
folderCommand.Add(folderScanCommand);
var sCommand = new Command("s", "(re)scan project folder for projects") { verboseOption, folderScanPathsArgument };
sCommand.Handler = folderScanCommandHandler;
rootCommand.Add(sCommand);
// project
var projectCommand = new Command("project", "Manage projects");
rootCommand.Add(projectCommand);
// project list
var projectListCommand = new Command("list", "List projects");
var projectListCommandHandler = CommandHandler.Create(async () =>
{
    var instance = await CLInstance.CreateAsync(await GetConfigurationAsync());
    foreach (var project in instance.Db.ProjectDirectoryProjects)
    {
        StringBuilder sb = new();
        if (project.Nickname is { } nick) sb.Append(nick).Append(" - ");
        sb.Append(CultureInfo.InvariantCulture, $"{project.FullPath} ({instance.GetPlatformName(project)} {instance.GetDisplayFramework(project)})");
        Console.WriteLine(sb.ToString());
    }
});
projectListCommand.Handler = projectListCommandHandler;
projectCommand.Add(projectListCommand);
var lCommand = new Command("l", "List projects");
lCommand.Handler = projectListCommandHandler;
rootCommand.Add(lCommand);
// project recent
var projectRecentCommand = new Command("recent", "List recent projects");
var projectRecentCommandHandler = CommandHandler.Create(async () =>
{
    var instance = await CLInstance.CreateAsync(await GetConfigurationAsync());
    foreach (var project in instance.Db.RecentProjects.OrderByDescending(v => v.OpenedTime))
    {
        StringBuilder sb = new();
        if (project.Nickname is { } nick) sb.Append(nick).Append(" - ");
        sb.Append(CultureInfo.InvariantCulture, $"{project.FullPath} ({instance.GetPlatformName(project)} {instance.GetDisplayFramework(project)})");
        Console.WriteLine(sb.ToString());
    }
});
projectRecentCommand.Handler = projectRecentCommandHandler;
projectCommand.Add(projectRecentCommand);
var rCommand = new Command("r", "List recent projects");
rCommand.Handler = projectRecentCommandHandler;
rootCommand.Add(rCommand);
// project launch
var projectLaunchProjectArgument = new Argument<string>("project", description: "Target project path or nick");
var projectLaunchInteractiveOption = new Option<bool>("--interactive", "Allow interactive remediations");
var projectLaunchCommand = new Command("launch", "Launch project") { projectLaunchProjectArgument, projectLaunchInteractiveOption };
var xCommand = new Command("x", "Launch project") { projectLaunchProjectArgument, projectLaunchInteractiveOption };
var projectLaunchCommandHandler = CommandHandler.Create(async (string project, bool interactive) =>
{
    var instance = await CLInstance.CreateAsync(await GetConfigurationAsync());
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
    var instance = await CLInstance.CreateAsync(await GetConfigurationAsync());
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
    var instance = await CLInstance.CreateAsync(await GetConfigurationAsync());
    BaseProjectModel? toNick = await instance.Db.ProjectDirectoryProjects.FindAsync(Path.GetFullPath(project));
    if (toNick != null)
    {
        toNick.Nickname = null;
        await instance.Db.SaveChangesAsync();
    }
});
projectCommand.Add(projectUnnickCommand);
// config
var configCommand = new Command("config", "Manage configuration");
rootCommand.Add(configCommand);
// config add
var configAddKeyArgument = new Argument<string>("key", description: "Config key");
var configAddValueArgument = new Argument<string>("value", description: "Config value");
var configAddCommand = new Command("add", "Add config key-value pair") { configAddKeyArgument, configAddValueArgument };
configAddCommand.Handler = CommandHandler.Create(async (string key, string value) =>
{
    var cfg = await GetConfigurationAsync();
    var dict = cfg.Options.ToDictionary(v => v.Key, v => v.Value);
    dict[key] = JsonSerializer.SerializeToElement(value);
    await WriteOptionsDefaultPathAsync(dict);
});
configCommand.Add(configAddCommand);
// config list
var configListCommand = new Command("list", "List config key-value pairs");
configListCommand.Handler = CommandHandler.Create(async () =>
{
    var cfg = await GetConfigurationAsync();
    foreach ((string key, JsonElement value) in cfg.Options)
        Console.WriteLine($"{key}={value.ToString()}");
});
configCommand.Add(configListCommand);
// config remove
var configRemoveKeyArgument = new Argument<string>("key", description: "Config key");
var configRemoveCommand = new Command("remove", "Remove config key-value pair") { configRemoveKeyArgument };
configRemoveCommand.Handler = CommandHandler.Create(async (string key) =>
{
    var cfg = await GetConfigurationAsync();
    var dict = cfg.Options.ToDictionary(v => v.Key, v => v.Value);
    dict.Remove(key);
    await WriteOptionsDefaultPathAsync(dict);
});
configCommand.Add(configRemoveCommand);
// config clear
var configClearCommand = new Command("clear", "Clear config");
configClearCommand.Handler = CommandHandler.Create(async () => await WriteOptionsDefaultPathAsync(ImmutableDictionary<string, JsonElement>.Empty));
configCommand.Add(configClearCommand);
// invoke
return await rootCommand.InvokeAsync(args);

static async Task<CLConfiguration> GetConfigurationAsync()
{
    var evaluators = TypeTool.CreateInstances<IProjectEvaluator>(TypeTool.GetConcreteInterfaceImplementors<IProjectEvaluator>(typeof(Anchor9)));
    var cfg = new CLConfiguration { Evaluators = evaluators, MaxRecentProjects = 10, MaxDepth = 2 };
    string cfgFile = Path.Combine(CLFiles.DataDirectory, "clconfig.json");
    if (!File.Exists(cfgFile))
    {
        Directory.CreateDirectory(CLFiles.DataDirectory);
        await WriteOptionsAsync(cfgFile, ImmutableDictionary<string, JsonElement>.Empty);
    }
    else
    {
        await using var stream = File.OpenRead(cfgFile);
        cfg.Options = await CLConfiguration.LoadOptionsAsync(stream);
    }
    return cfg;
}

static Task WriteOptionsDefaultPathAsync(IReadOnlyDictionary<string, JsonElement> options)
{
    string cfgFile = Path.Combine(CLFiles.DataDirectory, "clconfig.json");
    Directory.CreateDirectory(CLFiles.DataDirectory);
    return WriteOptionsAsync(cfgFile, options);
}

static async Task WriteOptionsAsync(string cfgFile, IReadOnlyDictionary<string, JsonElement> options)
{
    await using var stream = File.Create(cfgFile);
    await CLConfiguration.SerializeOptionsAsync(options, stream);
}
