namespace Futures.Internal;
internal sealed class AllCompletedPolicy<T> : IFutureAwaiterPolicy<T>, IFutureAwaiter<T>
{
    private readonly object _awaiterLock = new();
    private readonly ManualResetEvent _awaiterCond = new(false);
    private readonly Future<T>[] _futures;
    private int _uncompleted;

    public AllCompletedPolicy(params Future<T>[] futures)
    {
        _futures = futures.ToArray();
    }

    public ICompletableFuture<T>[] Futures => _futures;

    public void AddResult(Future<T> future) => this.Add(future);
    public void AddException(Future<T> future) => this.Add(future);
    public void AddCancellation(Future<T> future) => this.Add(future);

    private void Add(Future<T> _)
    {
        lock(_awaiterLock)
        {
            _uncompleted--;
            if (_uncompleted == 0)
            {
                _awaiterCond.Set();
            }
        }
    }

    public IReadOnlyCollection<Future<T>> Wait() => this.Wait(Timeout.InfiniteTimeSpan);

    public IReadOnlyCollection<Future<T>> Wait(TimeSpan timeout, Action<IFutureAwaiterPolicy<T>>? beforeWait = null)
    {
        var done = new List<ICompletableFuture<T>>();
        foreach (var future in Futures)
        {
            if (!future.Subscribe(this)) done.Add(future);
        }
        _uncompleted = _futures.Length - done.Count;
        if (_uncompleted == 0)
        {
            return _futures;
        }
        beforeWait?.Invoke(this);
        _awaiterCond.WaitOne(timeout);
        foreach (var future in Futures)
        {
            future.Unsubscribe(this);
        }
        return _futures;
    }
}
