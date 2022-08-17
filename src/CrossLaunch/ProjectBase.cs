namespace CrossLaunch;

public abstract record ProjectBase(string FullPath)
{
    public abstract string FrameworkString { get; }

    public abstract Task<ProjectLoadResult> TryLoadAsync(CLConfiguration configuration);
}
