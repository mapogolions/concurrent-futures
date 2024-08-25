using System.Diagnostics;

namespace Futures.Internal;

internal enum CompletionType
{
    Result,
    Exception,
    Cancellation
}

internal sealed class AllCompletedPolicy<T> : IFutureAwaiterPolicy<T>, IFutureAwaiter<T>
{
    private readonly object _awaiterLock = new();
    private readonly ManualResetEvent _awaiterCond = new(false);
    private readonly Future<T>[] _futures;
    private int _uncompleted;
    private readonly bool _stopOnException;

    public AllCompletedPolicy(params Future<T>[] futures) : this(futures, false) {}

    public AllCompletedPolicy(Future<T>[] futures, bool stopOnException)
    {
        _futures = futures.ToArray();
        _uncompleted = futures.Length;
        _stopOnException = stopOnException;
    }

    void IFutureAwaiter<T>.AddResult(Future<T> future) => this.Add(future, CompletionType.Result);
    void IFutureAwaiter<T>.AddException(Future<T> future) => this.Add(future, CompletionType.Exception);
    void IFutureAwaiter<T>.AddCancellation(Future<T> future) => this.Add(future, CompletionType.Cancellation);

    private void Add(Future<T> _, CompletionType completion)
    {
        lock(_awaiterLock)
        {
            _uncompleted--;
            if (_stopOnException && completion is CompletionType.Exception)
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

        // kind of optimization
        if (subscribers.Count == 0)
        {
            Debug.Assert(_uncompleted == 0);
            return _futures;
        }

        beforeWait?.Invoke(this);
        _awaiterCond.WaitOne(timeout);
        foreach (var subscriber in subscribers)
        {
            subscriber.Unsubscribe(this);
        }
        return _futures;

    }
}
