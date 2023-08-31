namespace Futures.Internal;

internal sealed partial class AllCompletedAwaiter<T> : IFutureAwaiter<T>
{
    private readonly ManualResetEvent _event = new(false);
    private readonly GroupLock _lock;
    private readonly ICompletableFuture<T>[] _futures;

    public AllCompletedAwaiter(params Future<T>[] futures)
    {
        _futures = futures.ToArray();
        _lock = new GroupLock(futures);
    }

    public IReadOnlyCollection<Future<T>> Wait() => this.Wait(Timeout.InfiniteTimeSpan);

    public IReadOnlyCollection<Future<T>> Wait(TimeSpan timeout)
    {
        _lock.Acquire();
        var done = _futures
            .Where(x => x.State is FutureState.Finished || x.State is FutureState.Cancelled)
            .Cast<Future<T>>()
            .ToList();
        if (done.Count == _futures.Length)
        {
            _lock.Release();
            return done;
        }
        var uncompleted = done.Count == 0 ? _futures : _futures.Except(done).ToArray();
        var policy = new AllCompletedAwaiterPolicy(this, uncompleted);
        _lock.Release();
        _event.WaitOne(timeout);
        done.AddRange(policy.Done());
        return done;
    }
}
