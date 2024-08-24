using Futures.Internal;

namespace Futures;

public class Future<T> : ICompletableFuture<T>
{
    private FutureState _state = FutureState.Pending;
    private T? _result;
    private Exception? _exception;
    private readonly Mutex _mutex = new();
    private readonly List<IFutureAwaiter<T>> _awaiters = new();

    public T? GetResult() => ((ICompletableFuture<T>)this).GetResult(Timeout.InfiniteTimeSpan, null);
    public T? GetResult(TimeSpan timeout) => ((ICompletableFuture<T>)this).GetResult(timeout, null);

    T? ICompletableFuture<T>.GetResult(TimeSpan timeout, Action<ICompletableFuture<T>>? beforeWait)
    {
        Monitor.Enter(_mutex);
        if (TryGetResulOrException(out var result, out Exception? ex))
        {
            return ReturnOrThrow(result, ex, finalize: () => Monitor.Exit(_mutex));
        }
        // `beforeWait` was introduced for testing purposes only
        beforeWait?.Invoke(this);
        // Pending or Running
        Monitor.Wait(_mutex, timeout);
        var finished = TryGetResulOrException(out result, out ex);
        return ReturnOrThrow(result, finished ? ex : new TimeoutException(), finalize: () => Monitor.Exit(_mutex));
    }

    private static T? ReturnOrThrow(T? result, Exception? ex, Action finalize)
    {
        try
        {
            if (ex != null) throw ex;
            return result;
        }
        finally
        {
            finalize();
        }
    }

    private bool TryGetResulOrException(out T? result, out Exception? ex)
    {
        result = default;
        ex = default;
        if (_state is FutureState.Cancelled || _state is FutureState.CancellationPropagated)
        {
            ex = new CancelledFutureException();
            return true;
        }
        if (_state is FutureState.Finished)
        {
            if (_exception != null)
            {
                ex = _exception;
            }
            else
            {
                result = _result;
            }
            return true;
        }
        return false;
    }

    public bool Cancel()
    {
        Monitor.Enter(_mutex);
        try
        {
            // There should be no way to cancel a running future
            if (_state is FutureState.Pending)
            {
                _state = FutureState.Cancelled;
                Monitor.PulseAll(_mutex);
                return true;
            }
            return _state is FutureState.Cancelled || _state is FutureState.CancellationPropagated;
        }
        finally
        {
            Monitor.Exit(_mutex);
        }
    }

    FutureState ICompletableFuture<T>.State => _state;
    void ILockable.Acquire() => Monitor.Enter(_mutex);
    void ILockable.Release() => Monitor.Exit(_mutex);

    bool ICompletableFuture<T>.Subscribe(IFutureAwaiter<T> awaiter)
    {
        Monitor.Enter(_mutex);
        try
        {
            var finished = _state is FutureState.Finished || _state is FutureState.CancellationPropagated;
            if (!finished)
            {
                _awaiters.Add(awaiter);
            }
            return !finished;
        } finally { Monitor.Exit(_mutex); }
    }

    bool ICompletableFuture<T>.Unsubscribe(IFutureAwaiter<T> awaiter)
    {
        Monitor.Enter(_mutex);
        try
        {
            var finished = _state is FutureState.Finished || _state is FutureState.CancellationPropagated;
            if (finished)
            {
                _awaiters.Remove(awaiter);
            }
            return finished;
        } finally { Monitor.Exit(_mutex); }
    }

    /**
     *  Used by ThreadPoolExecutor, which:
     *   - Runs pending futures; once started, a future cannot be canceled
     *   - Propagates cancellation to awaiting threads
     *   - Signals when a future is in an invalid state for execution
     */
    bool ICompletableFuture<T>.Run()
    {
        Monitor.Enter(_mutex);
        try
        {
            if (_state is FutureState.Pending)
            {
                _state = FutureState.Running;
                return true;
            }
            if (_state is FutureState.Cancelled)
            {
                _state = FutureState.CancellationPropagated;
                _awaiters.ForEach(x => x.AddCancellation(this));
                return false;
            }
            throw new InvalidFutureStateException($"Future in unexpected state: {_state}");
        }
        finally
        {
            Monitor.Exit( _mutex);
        }
    }

    /**
     *  Used by ThreadPoolExecutor to set the result of execution for a running or pending future.
     */
    void ICompletableFuture<T>.SetResult(T result) => this.Finish(_ =>
    {
        _result = result;
        _state = FutureState.Finished;
        _awaiters.ForEach(x => x.AddResult(this));
    });

    /**
     *  Used by ThreadPoolExecutor to set the exception that occurs during execution for a running or pending future.
     */
    void ICompletableFuture<T>.SetException(Exception exception) => this.Finish(_ =>
    {
        _exception = exception;
        _state = FutureState.Finished;
        _awaiters.ForEach(x => x.AddException(this));
    });

    private void Finish(Action<Future<T>> f)
    {
        Monitor.Enter(_mutex);
        try
        {
            if (_state is not FutureState.Pending && _state is not FutureState.Running)
            {
                throw new InvalidFutureStateException();
            }
            f(this);
            Monitor.PulseAll(_mutex);
        }
        finally
        {
            Monitor.Exit(_mutex);
        }
    }
}

public sealed class Future : Future<object>, ICompletableFuture
{
    public static IReadOnlyCollection<Future<R>> Wait<R>(FutureWaitPolicy policy, params Future<R>[] futures)
    {
        IFutureAwaiterPolicy<R> policy_ = policy switch
        {
            FutureWaitPolicy.FirstCompleted => new FirstCompletedPolicy<R>(futures),
            FutureWaitPolicy.AllCompleted => new AllCompletedPolicy<R>(futures),
            _ => throw new ArgumentOutOfRangeException()
        };
        return policy_.Wait();
    }

    public static IReadOnlyCollection<Future> Wait(FutureWaitPolicy policy, params Future[] futures)
    {
        var done = Wait<object>(policy, futures);
        return done.Cast<Future>().ToArray();
    }
}
