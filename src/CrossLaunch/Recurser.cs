namespace CrossLaunch;

public static class Recurser
{
    public static Task RecurseAsync(IEnumerable<string> inputs, Func<string, Task>? onFile, Func<string, Task>? onDirectory, int maxDepth = 1)
    {
        return RecurseAsync(inputs.Select(v => new EntryItem(File.Exists(v), v)), onFile, onDirectory, maxDepth);
    }

    private static async Task RecurseAsync(IEnumerable<EntryItem> inputs, Func<string, Task>? onFile, Func<string, Task>? onDirectory, int maxDepth = 1)
    {
        if (onFile == null && onDirectory == null) return;
        var dQueue = new Queue<RecursedRecord>();
        var fQueue = new Queue<RecursedRecord>();
        foreach ((bool isFile, string item) in inputs)
            (isFile ? fQueue : dQueue).Enqueue(new RecursedRecord(item, 0));
        RecursedRecord deq;
        while (true)
            if (fQueue.TryDequeue(out deq))
            {
                if (!File.Exists(deq.Path)) continue;
                if (onFile != null) await onFile(deq.Path).ConfigureAwait(false);
            }
            else if (dQueue.TryDequeue(out deq))
            {
                if (!Directory.Exists(deq.Path)) continue;
                if (onDirectory != null) await onDirectory(deq.Path).ConfigureAwait(false);
                int next = deq.Depth + 1;
                if (next > maxDepth) continue;
                if (onFile != null)
                    foreach (string file in Directory.EnumerateFiles(deq.Path))
                        fQueue.Enqueue(new RecursedRecord(file, next));
                foreach (string folder in Directory.EnumerateDirectories(deq.Path))
                    dQueue.Enqueue(new RecursedRecord(folder, next));
            }
            else break;
    }

    public static IEnumerable<EntryItem> Recurse(IEnumerable<string> inputs, int maxDepth = 1)
    {
        return Recurse(inputs.Select(v => new EntryItem(File.Exists(v), v)), maxDepth);
    }

    private static IEnumerable<EntryItem> Recurse(IEnumerable<EntryItem> inputs, int maxDepth = 1)
    {
        var dQueue = new Queue<RecursedRecord>();
        var fQueue = new Queue<RecursedRecord>();
        foreach ((bool isFile, string item) in inputs)
            (isFile ? fQueue : dQueue).Enqueue(new RecursedRecord(item, 0));
        RecursedRecord deq;
        while (true)
            if (fQueue.TryDequeue(out deq))
            {
                if (!File.Exists(deq.Path)) continue;
                yield return new EntryItem(true, deq.Path);
            }
            else if (dQueue.TryDequeue(out deq))
            {
                if (!Directory.Exists(deq.Path)) continue;
                yield return new EntryItem(false, deq.Path);
                int next = deq.Depth + 1;
                if (next > maxDepth) continue;
                foreach (string file in Directory.EnumerateFiles(deq.Path))
                    fQueue.Enqueue(new RecursedRecord(file, next));
                foreach (string folder in Directory.EnumerateDirectories(deq.Path))
                    dQueue.Enqueue(new RecursedRecord(folder, next));
            }
            else break;
    }

    private readonly record struct RecursedRecord(string Path, int Depth);
}

public readonly record struct EntryItem(bool IsFile, string Item);
