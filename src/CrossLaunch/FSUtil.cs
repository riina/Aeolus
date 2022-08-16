namespace CrossLaunch;

public static class FSUtil
{
    public static string? IfFileExists(string path) => File.Exists(path) ? path : null;
}
