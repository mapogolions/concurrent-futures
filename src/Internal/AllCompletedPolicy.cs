using System.Diagnostics;

namespace Futures.Internal;

internal sealed class AllCompletedPolicy<T> : IFutureAwaiterPolicy<T>
{
    private readonly Future<T>[] _futures;

    public AllCompletedPolicy(params Future<T>[] futures)
    {
        _futures = futures.ToArray();
    }

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

        // kind of optimization
        if (subscribers.Count == 0)
        {
            var done = awaiter.Done;
            Debug.Assert(done.Count == _futures.Length);
            return done;

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

    private sealed class Awaiter : IFutureAwaiter<T>
    {
        private readonly object _lock = new();
        private readonly ManualResetEventSlim _cond = new(false);
        private readonly AllCompletedPolicy<T> _policy;
        private readonly List<Future<T>> _completed = new();

        public Awaiter(AllCompletedPolicy<T> policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public void AddResult(Future<T> future) => this.Add(future);
        public void AddException(Future<T> future) => this.Add(future);
        public void AddCancellation(Future<T> future) => this.Add(future);

        private void Add(Future<T> future)
        {
            lock (_lock)
            {
                _completed.Add(future);
                if (_completed.Count == _policy._futures.Length)
                {
                    _cond.Set();
                }
            }
        }

        public void Wait(TimeSpan timeout) => _cond.Wait(timeout);

        public IReadOnlyCollection<Future<T>> Done => _completed;
    }
}
