namespace Futures.Internal;

internal sealed class AllCompletedAwaiter<T> : IFutureAwaiter<T>
{
    private readonly ManualResetEvent _event = new(false);
    private readonly GroupLock _groupLock;
    private readonly ICompletableFuture<T>[] _futures;

    public AllCompletedAwaiter(params Future<T>[] futures)
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
        if (done.Count == _futures.Length)
        {
            _groupLock.Release();
            return done;
        }
        var uncompleted = done.Count == 0 ? _futures : _futures.Except(done).ToArray();
        var policy = new AllCompletedAwaiterPolicy(this, uncompleted);
        _groupLock.Release();
        _event.WaitOne(timeout);
        done.AddRange(policy.Done());
        return done;
    }

    private sealed class AllCompletedAwaiterPolicy : IFutureAwaiterPolicy<T>
    {
        private readonly object _lock = new();
        private readonly List<Future<T>> _completed = new();
        private readonly AllCompletedAwaiter<T> _awaiter;
        private readonly IReadOnlyCollection<ICompletableFuture<T>> _uncompleted;

        public AllCompletedAwaiterPolicy(
            AllCompletedAwaiter<T> awaiter,
            IReadOnlyCollection<ICompletableFuture<T>> uncompleted)
        {
            _awaiter = awaiter ?? throw new ArgumentNullException(nameof(awaiter));
            _uncompleted = uncompleted ?? throw new ArgumentNullException(nameof(uncompleted));
            foreach (var future in _uncompleted)
            {
                future.AddPolicy(this);
            }
        }

        public IReadOnlyCollection<Future<T>> Done()
        {
            foreach (var future in _uncompleted)
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

        private void Add(Future<T> future)
        {
            lock(_lock)
            {
                _completed.Add(future);
                if (_completed.Count == _uncompleted.Count)
                {
                    _awaiter._event.Set();
                }
            }
        }
    }
}
