namespace CrossLaunch.Tests.Utility;

public class EnumerableAsyncWrapper<T> : IAsyncEnumerable<T>
{
    private readonly IEnumerable<T> _enumerable;

    public EnumerableAsyncWrapper(IEnumerable<T> enumerable) => _enumerable = enumerable;

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken()) =>
        new EnumeratorAsyncWrapper(_enumerable.GetEnumerator());

    private class EnumeratorAsyncWrapper : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _enumerator;
        private Task<bool> _task;

        public EnumeratorAsyncWrapper(IEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
            _task = Task.FromResult(false);
        }

        public ValueTask DisposeAsync() => new ValueTask(DisposeInternalAsync());

        private async Task DisposeInternalAsync()
        {
            await _task;
            _enumerator.Dispose();
        }

        public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_task = MoveNextInternalAsync(_task));

        private async Task<bool> MoveNextInternalAsync(Task task)
        {
            if (task.IsCompleted) await Task.Yield();
            else await task;
            return _enumerator.MoveNext();
        }

        public T Current => _enumerator.Current;
    }
}

public static class EnumerableAsyncExtensions
{
    public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IEnumerable<T> enumerable) =>
        new EnumerableAsyncWrapper<T>(enumerable);
}
