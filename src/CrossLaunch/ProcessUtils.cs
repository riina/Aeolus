using System.Diagnostics;

namespace CrossLaunch;

public static class ProcessUtils
{
    public static ProcessStartInfo GetUriStartInfo(string uri)
    {
        return new ProcessStartInfo(uri) { UseShellExecute = true };
    }

    public static Process StartUri(string uri)
    {
        return Process.Start(GetUriStartInfo(uri))!;
    }

    public static ProcessStartInfo GetStartInfo(string exe, params string[] args) => GetStartInfo(exe, (IEnumerable<string>)args);

    public static ProcessStartInfo GetStartInfo(string exe, IEnumerable<string> args)
    {
        var psi = new ProcessStartInfo(exe) { UseShellExecute = true };
        foreach (string arg in args)
            psi.ArgumentList.Add(arg);
        return psi;
    }

    public static Process Start(string exe, params string[] args) => Start(exe, (IEnumerable<string>)args);

    public static Process Start(string exe, IEnumerable<string> args)
    {
        return Process.Start(GetStartInfo(exe, args))!;
    }

    public static Func<Task> GetUriCallback(string uri) => () =>
    {
        ProcessUtils.StartUri(uri);
        return Task.CompletedTask;
    };
}
