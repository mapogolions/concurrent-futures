namespace Futures.Internal;

internal sealed class FirstCompletedPolicy<T> : IFutureAwaiterPolicy<T>
{
    private readonly Future<T>[] _futures;

    public FirstCompletedPolicy(params Future<T>[] futures)
    {
        _futures = futures.ToArray();
    }

    public IReadOnlyCollection<Future<T>> Wait() => this.Wait(Timeout.InfiniteTimeSpan);


    /**
    *  We follow the Python approach and do NOT break the loop when the first completed future is found.
    *  Instead, we continue iterating to find as many completed futures as possible.
    */
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
        // at least one future should be completed to step over
        awaiter.Wait(timeout);
        /**
         *  Even in the case of a timeout, the registered futures still have a small window of opportunity to notify `Awaiter` that they have completed. 
         *  Only after calling `Unsubscribe` for each future can we be certain that the `Awaiter._completed` collection will not change.
         */
        subscribers.ForEach(s => s.Unsubscribe(awaiter));
        return awaiter.Done;
    }

    private sealed class Awaiter : IFutureAwaiter<T>
    {
        private readonly object _lock = new();
        private readonly ManualResetEventSlim _cond = new(false);
        private readonly FirstCompletedPolicy<T> _policy;
        private readonly List<Future<T>> _completed = new();

        public Awaiter(FirstCompletedPolicy<T> policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public void AddResult(Future<T> future) => this.Add(future);
        public void AddException(Future<T> future) => this.Add(future);
        public void AddCancellation(Future<T> future) => this.Add(future);

        public void Add(Future<T> future)
        {
            lock(_lock)
            {
                _completed.Add(future);
                _cond.Set();
            }
        }

        public void Wait(TimeSpan timeout) => _cond.Wait(timeout);

        public IReadOnlyCollection<Future<T>> Done => _completed;
    }
}
