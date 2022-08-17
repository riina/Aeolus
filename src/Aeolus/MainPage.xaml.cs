using CrossLaunch.Ubiquitous;
using CrossLaunch;
using System.Collections.Immutable;
using System.Text.Json;
using Aeolus.ModelProxies;
using CrossLaunch.Models;

namespace Aeolus;

public partial class MainPage : ContentPage
{
    private readonly IFolderPicker _folderPicker;
    private readonly CLInstance _instance;

    public MainPage(IFolderPicker folderPicker)
    {
        InitializeComponent();
        _folderPicker = folderPicker;
        var fac = new AeolusDbContextFactory();
        var db = fac.CreateDbContext(Array.Empty<string>());
        var cfg = GetConfiguration();
        _instance = CLInstance.Create(cfg, db);
        projectList.ItemsSource = GetProjects(_instance.Db.ProjectDirectoryProjects);
    }

    private async void OnOpenFolderClicked(object sender, EventArgs e)
    {
        var picked = await _folderPicker.PickFolderAsync();
        if (picked != null)
        {
            var result = await _instance.AddDirectoryAsync(picked);
            if (result.Success) await _instance.UpdateDirectoryAsync(result.Model);
            projectList.ItemsSource = GetProjects(_instance.Db.ProjectDirectoryProjects);
        }
    }

    private List<ProjectDirectoryProject> GetProjects(IEnumerable<ProjectDirectoryProjectModel> models)
        => models.Select(v => new ProjectDirectoryProject
        {
            Name = Path.GetFileName(v.FullPath),
            FullPath = v.FullPath,
            SoftwareAndFramework = $"{_instance.GetPlatformName(v)} {_instance.GetDisplayFramework(v)}"
        }).ToList();

    static CLConfiguration GetConfiguration()
    {
        var evaluators = TypeTool.CreateInstances<IProjectEvaluator>(TypeTool.GetConcreteInterfaceImplementors<IProjectEvaluator>(typeof(Anchor9)));
        var cfg = new CLConfiguration { Evaluators = evaluators, MaxRecentProjects = 10, MaxDepth = 3 };
        string cfgFile = Path.Combine(AeolusFiles.DataDirectory, "clconfig.json");
        if (!File.Exists(cfgFile))
        {
            Directory.CreateDirectory(AeolusFiles.DataDirectory);
            WriteConfiguration(cfgFile, ImmutableDictionary<string, JsonElement>.Empty);
        }
        else
        {
            using var stream = File.OpenRead(cfgFile);
            cfg.Options = CLConfiguration.LoadOptions(stream);
        }
        return cfg;
    }

    static void WriteConfigurationDefaultPath(IReadOnlyDictionary<string, JsonElement> options)
    {
        string cfgFile = Path.Combine(AeolusFiles.DataDirectory, "clconfig.json");
        Directory.CreateDirectory(AeolusFiles.DataDirectory);
        WriteConfiguration(cfgFile, options);
    }

    static void WriteConfiguration(string cfgFile, IReadOnlyDictionary<string, JsonElement> options)
    {
        using var stream = File.Create(cfgFile);
        CLConfiguration.SerializeOptions(options, stream);
    }

}

