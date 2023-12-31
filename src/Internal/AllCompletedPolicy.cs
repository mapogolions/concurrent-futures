namespace Futures.Internal;

internal sealed partial class AllCompletedPolicy<T> : IFutureAwaiterPolicy<T>
{
    private readonly ManualResetEvent _event = new(false);
    private readonly GroupLock _lock;
    private readonly ICompletableFuture<T>[] _futures;

    public AllCompletedPolicy(params Future<T>[] futures)
    {
        _futures = futures.ToArray();
        _lock = new GroupLock(_futures);
    }

    public IReadOnlyCollection<Future<T>> Wait() => this.Wait(Timeout.InfiniteTimeSpan);

    public IReadOnlyCollection<Future<T>> Wait(TimeSpan timeout, Action<IFutureAwaiterPolicy<T>>? beforeWait = null)
    {
        _lock.Acquire();
        var done = _futures
            .Where(x => x.State is FutureState.Finished || x.State is FutureState.CancellationPropagated)
            .Cast<Future<T>>()
            .ToList();
        if (done.Count == _futures.Length)
        {
            _lock.Release();
            return done;
        }
        var uncompleted = done.Count == 0 ? _futures : _futures.Except(done).ToArray();
        var awaiter = new AllCompletedAwaiter(this, uncompleted);
        _lock.Release();
        beforeWait?.Invoke(this);
        _event.WaitOne(timeout);
        done.AddRange(awaiter.Done());
        return done;
    }
}
