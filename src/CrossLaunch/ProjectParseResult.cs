namespace CrossLaunch;

public record ProjectParseResult<T>(T? Result, ProjectParseFailInfo? FailInfo = null) where T : class
{
    public static ProjectParseResult<T> Missing => new(null, ProjectParseFailInfo.Missing);

    public static ProjectParseResult<T> InvalidFile => new(null, ProjectParseFailInfo.InvalidFile);
}

public record ProjectParseFailInfo(string Title, string ErrorMessage)
{
    public static readonly ProjectParseFailInfo Missing = new("Missing Files", "Project is missing required files");

    public static readonly ProjectParseFailInfo InvalidFile = new("Invalid Project File(s)", "Project file(s) could not be read.");

    public ProjectLoadResult AsProjectLoadResult() => new(false, new ProjectLoadFailInfo(Title, ErrorMessage, Array.Empty<ProjectLoadFailRemediation>()));
    public ProjectLoadFailInfo AsProjectLoadFailInfo() => new(Title, ErrorMessage, Array.Empty<ProjectLoadFailRemediation>());
}
