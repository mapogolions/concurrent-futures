using Futures.Internals;

namespace Futures.Awaiters;

public class FirstCompletedFutureAwaiter : FutureAwaiter
{
    private readonly object _lock = new();
    private readonly ManualResetEvent _event = new(false);
    private readonly Future<object>[] _futures;
    private readonly GroupLock _groupLock;

    public FirstCompletedFutureAwaiter(params Future<object>[] futures)
    {
        _futures = futures;
        _groupLock = new GroupLock(futures);
    }

    public IReadOnlyCollection<object> Wait()
    {
        _groupLock.Acquire();
        foreach (var future in _futures)
        {
            future.SubscribeUnsafe(this);
        }
        _groupLock.Release();
        _event.WaitOne();
        foreach (var future in _futures)
        {
            future.Acquire();
            future.UnsubscribeUnsafe(this);
            future.Release();
        }
        return Done.ToArray();
    }

    protected override void AddSuccess(Future<object> future)
    {
        lock(_lock) {
            base.AddSuccess(future);
            _event.Set();
        }
    }

    protected override void AddFailure(Future<object> future)
    {
        lock(_lock) {

            base.AddSuccess(future);
            _event.Set();
        }
    }
    protected override void AddCancellation(Future<object> future)
    {
        lock(_lock)
        {
            base.AddCancellation(future);
            _event.Set();
        }
    }
}