namespace Futures.Internal;

internal sealed class FirstCompletedAwaiter<T> : IFutureAwaiter<T>
{
    private readonly ManualResetEvent _event = new(false);
    private readonly GroupLock _groupLock;
    private readonly ICompletableFuture<T>[] _futures;

    public FirstCompletedAwaiter(params Future<T>[] futures)
    {
        _futures = futures.ToArray();
        _groupLock = new GroupLock(futures);
    }

    public IReadOnlyCollection<Future<T>> Wait() => this.Wait(Timeout.InfiniteTimeSpan);

    public IReadOnlyCollection<Future<T>> Wait(TimeSpan timeout)
    {
        _groupLock.Acquire();
        var done = _futures
            .Where(x => x.State is FutureState.Finished || x.State is FutureState.Cancelled)
            .Cast<Future<T>>()
            .ToList();
        if (done.Count > 0)
        {
            _groupLock.Release();
            return done;
        }
        // There are no completed futures, so listen to all of them
        var policy = new FirstCompletedAwaiterPolicy(this);
        _groupLock.Release();
        _event.WaitOne(timeout);
        return policy.Done();
    }

    private sealed class FirstCompletedAwaiterPolicy : IFutureAwaiterPolicy<T>
    {
        private readonly object _lock = new();
        private readonly List<Future<T>> _completed = new();
        private readonly FirstCompletedAwaiter<T> _awaiter;

        public FirstCompletedAwaiterPolicy(FirstCompletedAwaiter<T> awaiter)
        {
            _awaiter = awaiter ?? throw new ArgumentNullException(nameof(awaiter));
            foreach (var future in awaiter._futures)
            {
                future.AddPolicy(this);
            }
        }

        public IReadOnlyCollection<Future<T>> Done()
        {
            foreach (var future in _awaiter._futures)
            {
                future.Acquire();
                future.RemovePolicy(this);
                future.Release();
            }
            return _completed;
        }

        public void AddResult(Future<T> future) => this.Add(future);
        public void AddException(Future<T> future) => this.Add(future);
        public void AddCancellation(Future<T> future) => this.Add(future);

        public void Add(Future<T> future)
        {
            lock(_lock)
            {
                _completed.Add(future);
                _awaiter._event.Set();
            }
        }
    }
}
