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

    public bool Interactible
    {
        get => _interactible;
        private set
        {
            if (_interactible != value)
            {

                _interactible = value;
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

    public ProjectLoadFailInfo FailInfo
    {
        get => _failInfo;
        set
        {
            if (_failInfo != value)
            {
                _failInfo = value ?? ProjectLoadFailInfo.Unknown;
                OnPropertyChanged();
            }
        }
    }

    public readonly CLInstance CL;

    private List<ProjectDirectory> _projectDirectories = new();
    private List<ProjectDirectoryProject> _projectDirectoryProjects = new();
    private bool _busy;
    private bool _interactible = true;
    private ProjectLoadFailInfo _failInfo = ProjectLoadFailInfo.Unknown;
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
        await WaitOneAsync(_are);
        Interactible = false;
        Busy = true;
        var result = await CL.AddDirectoryAsync(picked);
        if (result.Success) await CL.UpdateDirectoryAsync(result.Model);
        UpdateProjectDirectories();
        UpdateProjectDirectoryProjects();
        Busy = false;
        Interactible = true;
        _are.Set();
    }

    public async Task RemoveProjectDirectoryAsync(string picked)
    {
        await WaitOneAsync(_are);
        Interactible = false;
        Busy = true;
        bool success = await CL.RemoveDirectoryAsync(picked);
        if (success)
        {
            UpdateProjectDirectories();
            UpdateProjectDirectoryProjects();
        }
        Busy = false;
        Interactible = true;
        _are.Set();
    }

    public async Task UpdateProjectDirectoryProjectsAsync()
    {
        await WaitOneAsync(_are);
        Interactible = false;
        Busy = true;
        await CL.UpdateAllDirectoriesAsync();
        UpdateProjectDirectoryProjects();
        Busy = false;
        Interactible = true;
        _are.Set();
    }

    public async Task LoadProjectAsync(string picked)
    {
        await WaitOneAsync(_are);
        Interactible = false;
        Busy = true;
        if (await CL.FindProjectAsync(picked) is { } project)
        {
            var result = await CL.LoadAsync(project);
            if (!result.Success)
            {
                FailInfo = result.FailInfo ?? ProjectLoadFailInfo.Unknown;
                await Shell.Current.GoToAsync("failed");
            }
        }
        Busy = false;
        Interactible = true;
        _are.Set();
    }

    private static CLConfiguration GetConfiguration()
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

    private static void WriteConfiguration(string cfgFile, IReadOnlyDictionary<string, JsonElement> options)
    {
        using var stream = File.Create(cfgFile);
        CLConfiguration.SerializeOptions(options, stream);
    }

    private static async Task WaitOneAsync(WaitHandle waitHandle, int msDelay = 10, int taskMsDelay = 10, CancellationToken cancellationToken = default)
    {
        while (!waitHandle.WaitOne(msDelay)) await Task.Delay(taskMsDelay, cancellationToken);
    }
}
