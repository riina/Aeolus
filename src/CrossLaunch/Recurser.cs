namespace CrossLaunch;

public static class Recurser
{
    public static Task RecurseAsync(IEnumerable<string> inputs, Func<string, Task>? onFile, Func<string, Task>? onDirectory)
    {
        return RecurseAsync(inputs.Select(v => new EntryItem(File.Exists(v), v)), onFile, onDirectory);
    }

    private static async Task RecurseAsync(IEnumerable<EntryItem> inputs, Func<string, Task>? onFile, Func<string, Task>? onDirectory)
    {
        if (onFile == null && onDirectory == null) return;
        var dQueue = new Queue<string>();
        var fQueue = new Queue<string>();
        foreach ((bool isFile, string item) in inputs)
            (isFile ? fQueue : dQueue).Enqueue(item);
        string? deq;
        while (true)
            if (fQueue.TryDequeue(out deq))
            {
                if (!File.Exists(deq)) continue;
                if (onFile != null) await onFile(deq).ConfigureAwait(false);
            }
            else if (dQueue.TryDequeue(out deq))
            {
                if (!Directory.Exists(deq)) continue;
                if (onDirectory != null) await onDirectory(deq).ConfigureAwait(false);
                if (onFile != null)
                    foreach (string file in Directory.EnumerateFiles(deq))
                        fQueue.Enqueue(file);
                foreach (string folder in Directory.EnumerateDirectories(deq))
                    dQueue.Enqueue(folder);
            }
            else break;
    }

    private readonly record struct EntryItem(bool IsFile, string Item);
}
