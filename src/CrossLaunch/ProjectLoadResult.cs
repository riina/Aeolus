namespace CrossLaunch;

public record ProjectLoadResult(bool Success, ProjectLoadFailInfo? FailInfo);

public record ProjectLoadFailInfo(string Title, string ErrorMessage, ProjectLoadFailRemediation[] Remediations);

public record ProjectLoadFailRemediation(string ActionShortName, string ActionDescription, Func<Task> Callback);
