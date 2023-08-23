using Futures.Internals;

namespace Futures.Awaiters;

public class FirstCompletedFutureAwaiter : FutureAwaiter
{
    private readonly object _lock = new();
    private readonly ManualResetEvent _event = new(false);
    private readonly ICompletableFuture[] _futures;
    private readonly GroupLock _groupLock;

    public FirstCompletedFutureAwaiter(params Future[] futures)
    {
        _futures = futures;
        _groupLock = new GroupLock(futures);
    }

    public IReadOnlyCollection<object> Wait() => this.Wait(Timeout.InfiniteTimeSpan);

    public IReadOnlyCollection<object> Wait(TimeSpan timeout)
    {
        using (_groupLock.Enter())
        {
            var done = _futures
                .Where(x => x.State is FutureState.Finished || x.State is FutureState.Cancelled)
                .ToList();
            if (done.Count > 0) return done;
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

    internal override void AddSuccess(Future future)
    {
        lock(_lock)
        {
            base.AddSuccess(future);
            _event.Set();
        }
    }

    internal override void AddFailure(Future future)
    {
        lock(_lock)
        {
            base.AddSuccess(future);
            _event.Set();
        }
    }
    internal override void AddCancellation(Future future)
    {
        lock(_lock)
        {
            base.AddCancellation(future);
            _event.Set();
        }
    }
}
