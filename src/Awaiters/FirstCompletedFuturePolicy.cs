using Futures.Internals;

namespace Futures.Awaiters;


public class FirstCompletedFuturePolicy : FirstCompletedFuturePolicy<object>
{
    public FirstCompletedFuturePolicy(params Future[] futures) : base(futures)
    {
    }
}


public class FirstCompletedFuturePolicy<T> : FutureAwaiter<T>
{
    private readonly object _lock = new();
    private readonly ManualResetEvent _event = new(false);
    private readonly ICompletableFuture<T>[] _futures;
    private readonly GroupLock _groupLock;

    public FirstCompletedFuturePolicy(params Future<T>[] futures)
    {
        _futures = futures;
        _groupLock = new GroupLock(futures);
    }

    public IReadOnlyCollection<Future<T>> Wait() => this.Wait(Timeout.InfiniteTimeSpan);

    public IReadOnlyCollection<Future<T>> Wait(TimeSpan timeout)
    {
        using (_groupLock.Enter())
        {
            var done = _futures
                .Where(x => x.State is FutureState.Finished || x.State is FutureState.Cancelled)
                .Cast<Future<T>>()
                .ToList();
            if (done.Count > 0)
            {
                return done;
            }
            foreach (var future in _futures)
            {
                future.SubscribeUnsafe(this);
            }
        }
        _event.WaitOne(timeout);
        foreach (var future in _futures)
        {
            future.Acquire();
            future.UnsubscribeUnsafe(this);
            future.Release();
        }
        return Done.ToArray();
    }

    internal override void AddSuccess(Future<T> future)
    {
        lock(_lock)
        {
            base.AddSuccess(future);
            _event.Set();
        }
    }

    internal override void AddFailure(Future<T> future)
    {
        lock(_lock)
        {
            base.AddSuccess(future);
            _event.Set();
        }
    }

    internal override void AddCancellation(Future<T> future)
    {
        lock(_lock)
        {
            base.AddCancellation(future);
            _event.Set();
        }
    }
}
