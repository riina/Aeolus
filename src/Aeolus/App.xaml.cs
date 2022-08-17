#nullable enable
using System.Collections.Immutable;
using System.Text.Json;
using CrossLaunch;
using CrossLaunch.Ubiquitous;

namespace Aeolus;

public partial class App : Application
{
    public static App? Me => Current as App;

    public readonly CLInstance CL;

    public App()
    {
        InitializeComponent();
        var fac = new AeolusDbContextFactory();
        var db = fac.CreateDbContext(Array.Empty<string>());
        var cfg = GetConfiguration();
        CL = CLInstance.Create(cfg, db);

        MainPage = new AppShell();
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
