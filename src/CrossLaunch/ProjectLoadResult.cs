namespace CrossLaunch;

public record ProjectLoadResult(bool Success, ProjectLoadFailInfo? FailInfo);

public record ProjectLoadFailInfo(string Title, string ErrorMessage, ProjectLoadFailRemediation? Remediation);

public record ProjectLoadFailRemediation(string ActionShortName, string ActionDescription, Func<Task> Callback);
