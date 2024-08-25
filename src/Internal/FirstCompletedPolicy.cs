namespace Futures.Internal;

internal sealed class FirstCompletedPolicy<T> : IFutureAwaiterPolicy<T>, IFutureAwaiter<T>
{
    private readonly object _awaiterLock = new();
    private readonly ManualResetEvent _awaiterCond = new(false);
    private readonly List<Future<T>> _completed = new();
    private readonly Future<T>[] _futures;

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


    /**
    *  We follow the Python approach and do NOT break the loop when the first completed future is found.
    *  Instead, we continue iterating to find as many completed futures as possible.
    */
    public IReadOnlyCollection<Future<T>> Wait(TimeSpan timeout, Action<IFutureAwaiterPolicy<T>>? beforeWait = null)
    {
        var subscribers = new List<ICompletableFuture<T>>();
        foreach (var future in _futures)
        {
            if (((ICompletableFuture<T>)future).Subscribe(this))
            {
                subscribers.Add(future);
            }
        }

        beforeWait?.Invoke(this);
        // at least one future should be completed to step over
        _awaiterCond.WaitOne(timeout);

        foreach (var subscriber in subscribers)
        {
            subscriber.Unsubscribe(this);
        }
        return _completed;
    }
}
