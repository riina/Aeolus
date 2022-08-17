#nullable enable
using System.Collections.Immutable;
using System.Text.Json;
using Aeolus.ModelProxies;
using CrossLaunch;
using CrossLaunch.Ubiquitous;

namespace Aeolus;

public partial class App : Application
{
    public static App? Me => Current as App;

    public bool Busy
    {
        get => _busy;
        private set
        {
            if (_busy != value)
            {

                _busy = value;
                OnPropertyChanged();
            }
        }
    }

    public List<ProjectDirectoryProject> ProjectDirectoryProjects
    {
        get => _projectDirectoryProjects;
        set
        {
            if (_projectDirectoryProjects != value)
            {

                _projectDirectoryProjects = value;
                OnPropertyChanged();
            }
        }
    }

    public List<ProjectDirectory> ProjectDirectories
    {
        get => _projectDirectories;
        set
        {
            if (_projectDirectories != value)
            {
                _projectDirectories = value;
                OnPropertyChanged();
            }
        }
    }

    public readonly CLInstance CL;

    private List<ProjectDirectory> _projectDirectories = new();
    private List<ProjectDirectoryProject> _projectDirectoryProjects = new();
    private bool _busy;
    private readonly AutoResetEvent _are = new(true);

    public App()
    {
        InitializeComponent();
        var fac = new AeolusDbContextFactory();
        var db = fac.CreateDbContext(Array.Empty<string>());
        var cfg = GetConfiguration();
        CL = CLInstance.Create(cfg, db);
        UpdateProjectDirectories();
        UpdateProjectDirectoryProjects();

        MainPage = new AppShell();
    }

    public void UpdateProjectDirectories()
    {
        ProjectDirectories = CL.GetProjectDirectories();
    }

    public void UpdateProjectDirectoryProjects()
    {
        ProjectDirectoryProjects = CL.GetProjectDirectoryProjects();
    }

    public async Task AddProjectDirectoryAsync(string picked)
    {
        _are.WaitOne();
        Busy = true;
        var result = await CL.AddDirectoryAsync(picked);
        if (result.Success) await CL.UpdateDirectoryAsync(result.Model);
        UpdateProjectDirectories();
        UpdateProjectDirectoryProjects();
        Busy = false;
        _are.Set();
    }

    public async Task RemoveProjectDirectoryAsync(string picked)
    {
        _are.WaitOne();
        Busy = true;
        bool success = await CL.RemoveDirectoryAsync(picked);
        if (success)
        {
            UpdateProjectDirectories();
            UpdateProjectDirectoryProjects();
        }
        Busy = false;
        _are.Set();
    }

    public async Task UpdateProjectDirectoryProjectsAsync()
    {
        _are.WaitOne();
        Busy = true;
        await CL.UpdateAllDirectoriesAsync();
        UpdateProjectDirectoryProjects();
        Busy = false;
        _are.Set();
    }

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
