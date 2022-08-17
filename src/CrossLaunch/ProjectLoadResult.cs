namespace CrossLaunch;

public record ProjectLoadResult(bool Success, ProjectLoadFailInfo? FailInfo)
{
    public static readonly ProjectLoadResult Successful = new(true, null);

    public static readonly ProjectLoadResult Unknown = new(false, ProjectLoadFailInfo.Unknown);

    public static readonly ProjectLoadResult InvalidFile = new(false, ProjectLoadFailInfo.InvalidFile);

    public static ProjectLoadResult Failure(ProjectLoadFailInfo? failInfo = null) => new(false, failInfo);

    public static ProjectLoadResult Failure(string title, string errorMessage, params ProjectLoadFailRemediation[] remediations) => new(false, new ProjectLoadFailInfo(title, errorMessage, remediations));

    public static ProjectLoadResult BadFrameworkId(string framework) => Failure("Invalid Framework ID", $"Could not process framework ID \"{framework}\"");
}

public record ProjectLoadFailInfo(string Title, string ErrorMessage, ProjectLoadFailRemediation[] Remediations)
{
    public static readonly ProjectLoadFailInfo Unknown = new("Unknown Error", "An unknown error occurred while reading the project.", Array.Empty<ProjectLoadFailRemediation>());

    public static readonly ProjectLoadFailInfo InvalidFile = new("Invalid Project File(s)", "Project file(s) could not be read.", Array.Empty<ProjectLoadFailRemediation>());
}

public record ProjectLoadFailRemediation(string ActionShortName, string ActionDescription, Func<Task> Callback);
