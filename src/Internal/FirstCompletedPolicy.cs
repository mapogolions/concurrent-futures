namespace Futures.Internal;

internal sealed partial class FirstCompletedPolicy<T> : IFutureAwaiterPolicy<T>
{
    private readonly ManualResetEvent _event = new(false);
    private readonly GroupLock _lock;
    private readonly ICompletableFuture<T>[] _futures;

    public FirstCompletedPolicy(params Future<T>[] futures)
    {
        _futures = futures.ToArray();
        _lock = new GroupLock(_futures);
    }

    public IReadOnlyCollection<Future<T>> Wait() => this.Wait(Timeout.InfiniteTimeSpan);

    public IReadOnlyCollection<Future<T>> Wait(TimeSpan timeout)
    {
        _lock.Acquire();
        var done = _futures
            .Where(x => x.State is FutureState.Finished || x.State is FutureState.Cancelled)
            .Cast<Future<T>>()
            .ToList();
        if (done.Count > 0)
        {
            _lock.Release();
            return done;
        }
        // There are no completed futures, so listen to all of them
        var awaiter = new FirstCompletedAwaiter(this);
        _lock.Release();
        _event.WaitOne(timeout);
        return awaiter.Done();
    }
}
