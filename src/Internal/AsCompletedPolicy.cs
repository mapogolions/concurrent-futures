using System.Collections;
using System.Diagnostics;

namespace Futures.Internal;

internal sealed class AsCompletedPolicy<T> : IFutureAwaiterPolicy<T>, IFutureAwaiter<T>
{
    private readonly object _awaiterLock = new();
    private readonly ManualResetEvent _awaiterCond = new(false);
    private readonly List<Future<T>> _completed = new();
    private readonly Future<T>[] _futures;
    private int _uncompleted;

    public AsCompletedPolicy(params Future<T>[] futures)
    {
        _futures = futures.ToArray();
        _uncompleted = _futures.Length;
    }

    void IFutureAwaiter<T>.AddResult(Future<T> future) => this.Add(future);
    void IFutureAwaiter<T>.AddException(Future<T> future) => this.Add(future);
    void IFutureAwaiter<T>.AddCancellation(Future<T> future) => this.Add(future);

    public void Add(Future<T> future)
    {
        lock(_awaiterLock)
        {
            _uncompleted--;
            _completed.Add(future);
            _awaiterCond.Set();
        }
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
        var subscribers = new List<ICompletableFuture<T>>();
        foreach (var future in _futures)
        {
            if (((ICompletableFuture<T>)future).Subscribe(this))
            {
                subscribers.Add(future);
            }
        }

        while (true)
        {
            if (_uncompleted == 0)
            {
                subscribers.ForEach(s => s.Unsubscribe(this));
                break;
            }
            beforeWait?.Invoke(this);
            _awaiterCond.WaitOne(timeout);
            IReadOnlyCollection<Future<T>>? chunk = null;
            lock (_awaiterLock)
            {
                _awaiterCond.Reset();
                chunk = _completed.ToArray();
            }
            yield return chunk!;
        }
        yield return _completed;
    }
}
