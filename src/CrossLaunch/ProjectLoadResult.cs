namespace CrossLaunch;

public record ProjectLoadResult(bool Success, ProjectLoadFailInfo? FailInfo)
{
    public static readonly ProjectLoadResult Successful = new(true, null);

    public static ProjectLoadResult Failure(ProjectLoadFailInfo? failInfo = null) => new(false, failInfo);

    public static ProjectLoadResult Failure(string title, string errorMessage, params ProjectLoadFailRemediation[] remediations) => new(false, new ProjectLoadFailInfo(title, errorMessage, remediations));
}

public record ProjectLoadFailInfo(string Title, string ErrorMessage, ProjectLoadFailRemediation[] Remediations);

public record ProjectLoadFailRemediation(string ActionShortName, string ActionDescription, Func<Task> Callback);
