namespace CrossLaunch;

public record ProjectLoadResult(bool Success, ProjectLoadFailInfo? FailInfo)
{
    public static readonly ProjectLoadResult Successful = new(true, null);

    public static readonly ProjectLoadResult Unknown = new(false, new ProjectLoadFailInfo("Unknown Error", "An unknown error occurred while reading the project.", Array.Empty<ProjectLoadFailRemediation>()));

    public static readonly ProjectLoadResult InvalidFile = new(false, new ProjectLoadFailInfo("Invalid Project File(s)", "Project file(s) could not be read.", Array.Empty<ProjectLoadFailRemediation>()));

    public static ProjectLoadResult Failure(ProjectLoadFailInfo? failInfo = null) => new(false, failInfo);

    public static ProjectLoadResult Failure(string title, string errorMessage, params ProjectLoadFailRemediation[] remediations) => new(false, new ProjectLoadFailInfo(title, errorMessage, remediations));

    public static ProjectLoadResult BadFrameworkId(string framework) => Failure("Invalid Framework ID", $"Could not process framework ID \"{framework}\"");
}

public record ProjectLoadFailInfo(string Title, string ErrorMessage, ProjectLoadFailRemediation[] Remediations);

public record ProjectLoadFailRemediation(string ActionShortName, string ActionDescription, Func<Task> Callback);
