namespace Futures.Internal;

internal sealed class AsCompletedPolicy<T> : IFutureAwaiterPolicy<T>
{
    private readonly Future<T>[] _futures;

    public AsCompletedPolicy(params Future<T>[] futures)
    {
        _futures = futures.ToArray();
    }

    public IReadOnlyCollection<Future<T>> Wait() => this.Wait(Timeout.InfiniteTimeSpan);


    public IReadOnlyCollection<Future<T>> Wait(TimeSpan timeout, Action<IFutureAwaiterPolicy<T>>? beforeWait = null)
    {
        var done = new List<Future<T>>();
        foreach (var chunk in AsEnumerable(timeout, beforeWait))
        {
            done.AddRange(chunk);
        }
        return _futures;
    }

    public IEnumerable<IReadOnlyCollection<Future<T>>> AsEnumerable(TimeSpan timeout, Action<IFutureAwaiterPolicy<T>>? beforeWait = null)
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

        while (true)
        {
            beforeWait?.Invoke(this);
            if (!awaiter.Wait(timeout))
            {
                /**
                 *  Even in the case of a timeout, the registered futures still have a small window of opportunity to notify `Awaiter` that they have completed. 
                 *  Only after calling `Unsubscribe` for each future can we be certain that the `Awaiter._completed` collection will not change.
                 */
                subscribers.ForEach(s => s.Unsubscribe(awaiter));
                if (awaiter.Done.Count != 0) yield return awaiter.Done;
                yield break;
            }
            if (awaiter.MoveNext(out var done))
            {
                yield return done;
                continue;
            }
            subscribers.ForEach(s => s.Unsubscribe(awaiter));
            yield return done;
            yield break;
        }
    }

    private sealed class Awaiter : IFutureAwaiter<T>
    {
        private readonly object _lock = new();
        private readonly ManualResetEvent _cond = new(false);
        private readonly AsCompletedPolicy<T> _policy;
        private readonly List<Future<T>> _completed = new();
        private int _uncompleted;

        public Awaiter(AsCompletedPolicy<T> policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _uncompleted = policy._futures.Length;
        }

        public void AddResult(Future<T> future) => this.Add(future);
        public void AddException(Future<T> future) => this.Add(future);
        public void AddCancellation(Future<T> future) => this.Add(future);

        private void Add(Future<T> future)
        {
            lock(_lock)
            {
                _uncompleted--;
                _completed.Add(future);
                _cond.Set();
            }
        }

        public bool Wait(TimeSpan timeout) => _cond.WaitOne(timeout);

        public bool MoveNext(out IReadOnlyCollection<Future<T>> done)
        {
            lock (_lock)
            {
                var hasNext = _uncompleted > 0;
                if (hasNext)
                {
                    _cond.Reset();
                }
                done = _completed.ToArray();
                _completed.Clear();
                return hasNext;
            }
        }

        public IReadOnlyCollection<Future<T>> Done => _completed;
    }
}
