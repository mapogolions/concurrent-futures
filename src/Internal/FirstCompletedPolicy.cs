namespace Futures.Internal;

internal sealed class FirstCompletedPolicy<T> : IFutureAwaiterPolicy<T>, IFutureAwaiter<T>
{
    private readonly object _awaiterLock = new();
    private readonly ManualResetEvent _awaiterCond = new(false);
    private readonly List<Future<T>> _completed = new();
    private readonly ICompletableFuture<T>[] _futures;

    public FirstCompletedPolicy(params Future<T>[] futures)
    {
        _futures = futures.ToArray();
    }

    void IFutureAwaiter<T>.AddResult(Future<T> future) => this.Add(future);
    void IFutureAwaiter<T>.AddException(Future<T> future) => this.Add(future);
    void IFutureAwaiter<T>.AddCancellation(Future<T> future) => this.Add(future);

    public void Add(Future<T> future)
    {
        lock(_awaiterLock)
        {
            _completed.Add(future);
            _awaiterCond.Set();
        }
    }

    public IReadOnlyCollection<Future<T>> Wait() => this.Wait(Timeout.InfiniteTimeSpan);


    public IReadOnlyCollection<Future<T>> Wait(TimeSpan timeout, Action<IFutureAwaiterPolicy<T>>? beforeWait = null)
    {
        var done = new List<ICompletableFuture<T>>();
        foreach (var future in _futures)
        {
             /**
              *  We follow the Python approach and do NOT break the loop when the first completed future is found.
              *  Instead, we continue iterating to find as many completed futures as possible.
              */
            if (!future.Subscribe(this)) done.Add(future);
        }
        if (done.Count > 0)
        {
            return done.Cast<Future<T>>().ToList();
        }
        // There are no completed futures, so listen to all of them
        beforeWait?.Invoke(this);
        _awaiterCond.WaitOne(timeout);
        foreach (var future in _futures)
        {
            future.Unsubscribe(this);
        }
        return _completed;
    }
}
