namespace CrossLaunch;

public class CLConfiguration
{
    public IProjectEvaluator[] Evaluators { get; set; } = Array.Empty<IProjectEvaluator>();

    public int MaxRecentProjects { get; set; } = 10;

    public int MaxDepth { get; set; } = 1;
}
