namespace Aeolus;

public static class AeolusFiles
{
    static AeolusFiles()
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
        DataDirectory = Path.Combine(baseDir, "AeolusCL");
    }

    public static readonly string DataDirectory;
}
