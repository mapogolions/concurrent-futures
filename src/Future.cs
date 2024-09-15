using Futures.Internal;

namespace Futures;

public class Future<T> : ICompletableFuture<T>
{
    private FutureState _state = FutureState.Pending;
    private T? _result;
    private Exception? _exception;
    private readonly Condition _cond = new();
    private readonly List<IFutureAwaiter<T>> _awaiters = new();

    internal Future() {}

    public T? GetResult() => ((ICompletableFuture<T>)this).GetResult(Timeout.InfiniteTimeSpan, null);
    public T? GetResult(TimeSpan timeout) => ((ICompletableFuture<T>)this).GetResult(timeout, null);

    T? ICompletableFuture<T>.GetResult(TimeSpan timeout, Action<ICompletableFuture<T>>? beforeWait)
    {
        _cond.Acquire();
        if (TryGetResulOrException(out var result, out Exception? ex))
        {
            return ReturnOrThrow(result, ex, finalize: _cond.Release);
        }
        // `beforeWait` was introduced for testing purposes only
        beforeWait?.Invoke(this);

        // Pending or Running
        _cond.Wait(timeout);

        var finished = TryGetResulOrException(out result, out ex);
        return ReturnOrThrow(result, finished ? ex : new TimeoutException(), finalize: _cond.Release);
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
        _cond.Acquire();
        try
        {
            // There should be no way to cancel a running future
            if (_state is FutureState.Pending)
            {
                _state = FutureState.Cancelled;
                _cond.NotifyAll();
                return true;
            }
            return _state is FutureState.Cancelled || _state is FutureState.CancellationPropagated;
        }
        finally { _cond.Release(); }
    }

    public bool Running()
    {
        _cond.Acquire();
        try
        {
            return _state is FutureState.Running;
        }
        finally { _cond.Release(); }
    }

    public bool Cancelled()
    {
        _cond.Acquire();
        try
        {
            return _state is FutureState.Cancelled || _state is FutureState.CancellationPropagated;
        }
        finally { _cond.Release(); }
    }

    FutureState ICompletableFuture<T>.State => _state;

    // Used by `IFutureAwaiter`
    bool ICompletableFuture<T>.Subscribe(IFutureAwaiter<T> awaiter)
    {
        _cond.Acquire();
        try
        {
            if (_state is FutureState.CancellationPropagated)
            {
                awaiter.AddCancellation(this);
                return false;
            }
            if (_state is FutureState.Finished)
            {
                if (_exception is not null) awaiter.AddException(this);
                else awaiter.AddResult(this);
                return false;
            }
            _awaiters.Add(awaiter);
            return true;
        }
        finally { _cond.Release(); }
    }

    bool ICompletableFuture<T>.Unsubscribe(IFutureAwaiter<T> awaiter)
    {
        _cond.Acquire();
        try
        {
            return _awaiters.Remove(awaiter);
        }
        finally { _cond.Release(); }
    }

    /**
     *  Used by ThreadPoolExecutor, which:
     *   - Runs pending futures; once started, a future cannot be canceled
     *   - Propagates cancellation to awaiting threads
     *   - Signals when a future is in an invalid state for execution
     */
    bool ICompletableFuture<T>.Run()
    {
        _cond.Acquire();
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
        finally { _cond.Release(); }
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
        _cond.Acquire();
        try
        {
            if (_state is not FutureState.Pending && _state is not FutureState.Running)
            {
                throw new InvalidFutureStateException();
            }
            f(this);
            _cond.NotifyAll();
        }
        finally { _cond.Release(); }
    }
}


public sealed class Future : Future<object>, ICompletableFuture
{
    internal Future() {}

    public static IEnumerable<Future<R>> AsCompleted<R>(params Future<R>[] futures) => AsCompleted(Timeout.InfiniteTimeSpan, futures);

    public static IEnumerable<Future<R>> AsCompleted<R>(TimeSpan timeout, params Future<R>[] futures)
    {
        var policy = new AsCompletedPolicy<R>(futures);
        foreach (var done in policy.AsEnumerable(timeout))
        {
            foreach (var future in done)
            {
                yield return future;
            }
        }
    }

    public static IReadOnlyCollection<Future<R>> Wait<R>(FutureWaitPolicy policy, params Future<R>[] futures) =>
        Wait(Timeout.InfiniteTimeSpan, policy, futures);

    public static IReadOnlyCollection<Future<R>> Wait<R>(TimeSpan timeout, FutureWaitPolicy policy, params Future<R>[] futures)
    {
        IFutureAwaiterPolicy<R> policy_ = policy switch
        {
            FutureWaitPolicy.FirstCompleted => new FirstCompletedPolicy<R>(futures),
            FutureWaitPolicy.AllCompleted => new AllCompletedPolicy<R>(futures),
            FutureWaitPolicy.FirstException => new FirstExceptionPolicy<R>(futures),
            _ => throw new ArgumentOutOfRangeException()
        };
        return policy_.Wait(timeout);
    }

    public static IReadOnlyCollection<Future> Wait(FutureWaitPolicy policy, params Future[] futures) =>
        Wait(Timeout.InfiniteTimeSpan, policy, futures);

    public static IReadOnlyCollection<Future> Wait(TimeSpan timeout, FutureWaitPolicy policy, params Future[] futures)
    {
        var done = Wait<object>(timeout, policy, futures);
        return done.Cast<Future>().ToArray();
    }
}
