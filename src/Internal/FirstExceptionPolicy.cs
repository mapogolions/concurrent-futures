namespace Futures.Internal;

internal enum CompletionType
{
    Result,
    Exception,
    Cancellation
}

internal sealed class FirstExceptionPolicy<T>(params Future<T>[] futures) : IFutureAwaiterPolicy<T>
{
    private readonly Future<T>[] _futures = [.. futures];

    public IReadOnlyCollection<Future<T>> Wait() => this.Wait(Timeout.InfiniteTimeSpan);

    public IReadOnlyCollection<Future<T>> Wait(TimeSpan timeout, Action<IFutureAwaiterPolicy<T>>? beforeWait = null)
    {
        var awaiter = new Awaiter(this);
        var subscribers = new List<ICompletableFuture<T>>();
        foreach (var future in _futures)
        {
            if (((ICompletableFuture<T>)future).Subscribe(awaiter))
            {
                subscribers.Add(future);
            }
        }

        beforeWait?.Invoke(this);
        awaiter.Wait(timeout);
        /**
         *  Even in the case of a timeout, the registered futures still have a small window of opportunity to notify `Awaiter` that they have completed. 
         *  Only after calling `Unsubscribe` for each future can we be certain that the `Awaiter._completed` collection will not change.
         */
        subscribers.ForEach(s => s.Unsubscribe(awaiter));
        return awaiter.Done;
    }

    private sealed class Awaiter(FirstExceptionPolicy<T> policy) : IFutureAwaiter<T>
    {
        private readonly Lock _lock = new();
        private readonly ManualResetEventSlim _cond = new(false);
        private readonly List<Future<T>> _completed = [];

        public void AddResult(Future<T> future) => this.Add(future, CompletionType.Result);
        public void AddException(Future<T> future) => this.Add(future, CompletionType.Exception);
        public void AddCancellation(Future<T> future) => this.Add(future, CompletionType.Cancellation);

        private void Add(Future<T> future, CompletionType completion)
        {
            lock(_lock)
            {
                _completed.Add(future);
                if (completion is CompletionType.Exception)
                {
                    _cond.Set();
                    return;
                }
                if (_completed.Count == policy._futures.Length)
                {
                    _cond.Set();
                }
            }
        }

        public void Wait(TimeSpan timeout) => _cond.Wait(timeout);

        public IReadOnlyCollection<Future<T>> Done => _completed;
    }
}
