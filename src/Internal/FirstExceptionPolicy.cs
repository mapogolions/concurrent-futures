namespace Futures.Internal;

internal enum CompletionType
{
    Result,
    Exception,
    Cancellation
}

internal sealed class FirstExceptionPolicy<T> : IFutureAwaiterPolicy<T>, IFutureAwaiter<T>
{
    private readonly object _awaiterLock = new();
    private readonly ManualResetEvent _awaiterCond = new(false);
    private readonly Future<T>[] _futures;
    private readonly List<Future<T>> _completed = new();
    private int _uncompleted;

    public FirstExceptionPolicy(params Future<T>[] futures)
    {
        _futures = futures.ToArray();
        _uncompleted = futures.Length;
    }

    void IFutureAwaiter<T>.AddResult(Future<T> future) => this.Add(future, CompletionType.Result);
    void IFutureAwaiter<T>.AddException(Future<T> future) => this.Add(future, CompletionType.Exception);
    void IFutureAwaiter<T>.AddCancellation(Future<T> future) => this.Add(future, CompletionType.Cancellation);

    private void Add(Future<T> future, CompletionType completion)
    {
        lock(_awaiterLock)
        {
            _uncompleted--;
            _completed.Add(future);
            if (completion is CompletionType.Exception)
            {
                _awaiterCond.Set();
                return;
            }
            if (_uncompleted == 0)
            {
                _awaiterCond.Set();
            }
        }
    }

    public IReadOnlyCollection<Future<T>> Wait() => this.Wait(Timeout.InfiniteTimeSpan);

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
        _awaiterCond.WaitOne(timeout);

        foreach (var subscriber in subscribers)
        {
            subscriber.Unsubscribe(this);
        }
        return _completed;
    }
}
