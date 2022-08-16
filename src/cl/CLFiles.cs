namespace cl;

public static class CLFiles
{
    static CLFiles()
    {
        // macOS: .config
        // Windows: AppData/Roaming
        string baseDir;
        try
        {
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify);
            if (baseDir == "") baseDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify);
        }
        catch
        {
            baseDir = Environment.CurrentDirectory;
        }
        DataDirectory = Path.Combine(baseDir, "CrossLaunchCL");
    }

    public static readonly string DataDirectory;
}
