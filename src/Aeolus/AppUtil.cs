namespace Aeolus;

public static class DbUtil
{
    static DbUtil()
    {
        AppPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KurtAmbrose", "aeolus");
        DatabasePath = Path.Join(AppPath, "db.db");
    }

    public static readonly string AppPath;

    public static readonly string DatabasePath;
}
