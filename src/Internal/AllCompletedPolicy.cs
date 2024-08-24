using System.Diagnostics;

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
        _uncompleted = futures.Length;
    }

    void IFutureAwaiter<T>.AddResult(Future<T> future) => this.Add(future);
    void IFutureAwaiter<T>.AddException(Future<T> future) => this.Add(future);
    void IFutureAwaiter<T>.AddCancellation(Future<T> future) => this.Add(future);

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
