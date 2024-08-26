namespace Futures.Internal;

internal enum CompletionType
{
    Result,
    Exception,
    Cancellation
}

internal sealed class FirstExceptionPolicy<T> : IFutureAwaiterPolicy<T>
{
    private readonly Future<T>[] _futures;

    public FirstExceptionPolicy(params Future<T>[] futures)
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

        beforeWait?.Invoke(this);
        awaiter.Wait(timeout);

        subscribers.ForEach(s => s.Unsubscribe(awaiter));
        return awaiter.Done;
    }

    private sealed class Awaiter : IFutureAwaiter<T>
    {
        private readonly object _lock = new();
        private readonly ManualResetEvent _cond = new(false);
        private readonly FirstExceptionPolicy<T> _policy;
        private readonly List<Future<T>> _completed = new();
        private int _uncompleted;

        public Awaiter(FirstExceptionPolicy<T> policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _uncompleted = policy._futures.Length;
        }

        public void AddResult(Future<T> future) => this.Add(future, CompletionType.Result);
        public void AddException(Future<T> future) => this.Add(future, CompletionType.Exception);
        public void AddCancellation(Future<T> future) => this.Add(future, CompletionType.Cancellation);

        private void Add(Future<T> future, CompletionType completion)
        {
            lock(_lock)
            {
                _uncompleted--;
                _completed.Add(future);
                if (completion is CompletionType.Exception)
                {
                    _cond.Set();
                    return;
                }
                if (_uncompleted == 0)
                {
                    _cond.Set();
                }
            }
        }

        public void Wait(TimeSpan timeout) => _cond.WaitOne(timeout);

        public IReadOnlyCollection<Future<T>> Done => _completed;
    }
}
