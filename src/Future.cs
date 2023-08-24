using Futures.Internal;

namespace Futures;

public class Future<T> : ICompletableFuture<T>
{
    private FutureState _state = FutureState.Pending;
    private T? _result;
    private Exception? _exception;
    private readonly Mutex _mutex = new();
    private readonly List<IFutureAwaiterPolicy<T>> _policies = new();

    public T? GetResult() => GetResult(Timeout.InfiniteTimeSpan);

    public T? GetResult(TimeSpan timeout)
    {
        Monitor.Enter(_mutex);
        if (_state is FutureState.Cancelled)
        {
            throw new CancelledFutureException();
        }
        if (_state is FutureState.Finished)
        {
            var result = ResultUnsafe();
            Monitor.Exit(_mutex);
            return result;
        }

        // Pending or Running => wait
        Monitor.Wait(_mutex, timeout);

        if (_state is FutureState.Cancelled)
        {
            throw new CancelledFutureException();
        }
        if (_state is FutureState.Finished)
        {
            var result = ResultUnsafe();
            Monitor.Exit(_mutex);
            return result;
        }
        throw new TimeoutException();
    }

    public bool Cancel()
    {
        Monitor.Enter(_mutex);
        if (_state is FutureState.Pending)
        {
            _state = FutureState.Cancelled;
            Monitor.PulseAll(_mutex);
            Monitor.Exit(_mutex);
            return true;
        }
        var state = _state;
        Monitor.Exit(_mutex);
        return state is FutureState.Cancelled;
    }

    private T? ResultUnsafe() => _exception is null ? _result : throw _exception;

    FutureState ICompletableFuture<T>.State => _state;
    void ILockable.Acquire() => Monitor.Enter(_mutex);
    void ILockable.Release() => Monitor.Exit(_mutex);

    void ICompletableFuture<T>.AddPolicy(IFutureAwaiterPolicy<T> policy) => _policies.Add(policy);
    void ICompletableFuture<T>.RemovePolicy(IFutureAwaiterPolicy<T> policy) => _policies.Remove(policy);

    void ICompletableFuture<T>.SetResult(T result) => this.Finish(x =>
    {
        x._result = result;
        _state = FutureState.Finished;
        _policies.ForEach(x => x.AddResult(this));
    });

    void ICompletableFuture<T>.SetException(Exception exception) => this.Finish(x =>
    {
        x._exception = exception;
        _state = FutureState.Finished;
        _policies.ForEach(x => x.AddException(this));
    });

    private void Finish(Action<Future<T>> f)
    {
        Monitor.Enter(_mutex);
        if (_state is FutureState.Cancelled || _state is FutureState.Finished)
        {
            throw new InvalidFutureStateException();
        }
        f(this);
        Monitor.PulseAll(_mutex);
        Monitor.Exit(_mutex);
    }
}

public class Future : Future<object>, ICompletableFuture { }
