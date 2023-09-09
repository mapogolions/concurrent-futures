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
        if (_state is FutureState.Cancelled || _state is FutureState.CancellationPropagated)
        {
            throw new CancelledFutureException();
        }
        if (_state is FutureState.Finished)
        {
            var result = GetResultUnsafe();
            Monitor.Exit(_mutex);
            return result;
        }

        // `beforeWait` was introduced for testing purposes only.
        // This helps to write reliable tests that do not depend on context switching between threads.
        beforeWait?.Invoke(this);
        // Pending or Running => wait
        Monitor.Wait(_mutex, timeout);

        if (_state is FutureState.Cancelled || _state is FutureState.CancellationPropagated)
        {
            throw new CancelledFutureException();
        }
        if (_state is FutureState.Finished)
        {
            var result = GetResultUnsafe();
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
        return state is FutureState.Cancelled || state is FutureState.CancellationPropagated;
    }

    private T? GetResultUnsafe() => _exception is null ? _result : throw _exception;

    FutureState ICompletableFuture<T>.State => _state;
    void ILockable.Acquire() => Monitor.Enter(_mutex);
    void ILockable.Release() => Monitor.Exit(_mutex);

    void ICompletableFuture<T>.Subscribe(IFutureAwaiter<T> awaiter) => _awaiters.Add(awaiter);
    void ICompletableFuture<T>.Unsubscribe(IFutureAwaiter<T> awaiter) => _awaiters.Remove(awaiter);

    bool ICompletableFuture<T>.Run()
    {
        Monitor.Enter(_mutex);
        if (_state is FutureState.Pending)
        {
            _state = FutureState.Running;
            Monitor.Exit(_mutex);
            return true;
        }
        if (_state is FutureState.Cancelled)
        {
            _state = FutureState.CancellationPropagated;
            _awaiters.ForEach(x => x.AddCancellation(this));
            Monitor.Exit(_mutex);
            return false;
        }
        throw new InvalidFutureStateException($"Future in unexpected state: {_state}");
    }

    void ICompletableFuture<T>.SetResult(T result) => this.Finish(_ =>
    {
        _result = result;
        _state = FutureState.Finished;
        _awaiters.ForEach(x => x.AddResult(this));
    });

    void ICompletableFuture<T>.SetException(Exception exception) => this.Finish(_ =>
    {
        _exception = exception;
        _state = FutureState.Finished;
        _awaiters.ForEach(x => x.AddException(this));
    });

    private void Finish(Action<Future<T>> f)
    {
        Monitor.Enter(_mutex);
        if (_state is FutureState.Cancelled  || _state is FutureState.CancellationPropagated || _state is FutureState.Finished)
        {
            throw new InvalidFutureStateException();
        }
        f(this);
        Monitor.PulseAll(_mutex);
        Monitor.Exit(_mutex);
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
