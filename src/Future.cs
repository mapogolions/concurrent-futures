using System.ComponentModel;
using Futures.Internal;

namespace Futures;

public class Future<T> : ICompletableFuture<T>
{
    private FutureState _state = FutureState.Pending;
    private T? _result;
    private Exception? _exception;
    private readonly Mutex _mutex = new();
    private readonly List<IFutureAwaiter<T>> _awaiters = new();

    public T? GetResult() => GetResult(Timeout.InfiniteTimeSpan);

    public T? GetResult(TimeSpan timeout)
    {
        Monitor.Enter(_mutex);
        if (_state is FutureState.Cancelled || _state is FutureState.CancellationPropagated)
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

        if (_state is FutureState.Cancelled || _state is FutureState.CancellationPropagated)
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

    void ICompletableFuture<T>.Subscribe(IFutureAwaiter<T> awaiter) => _awaiters.Add(awaiter);
    void ICompletableFuture<T>.Unsubscribe(IFutureAwaiter<T> awaiter) => _awaiters.Remove(awaiter);

    bool ICompletableFuture<T>.RunOrPropagate()
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

    void ICompletableFuture<T>.SetResult(T result) => this.Finish(x =>
    {
        x._result = result;
        _state = FutureState.Finished;
        _awaiters.ForEach(x => x.AddResult(this));
    });

    void ICompletableFuture<T>.SetException(Exception exception) => this.Finish(x =>
    {
        x._exception = exception;
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

    public static IReadOnlyCollection<Future<R>> Wait<R>(FutureWaitPolicy policy, params Future<R>[] futures)
    {
        IFutureAwaiterPolicy<R> policy_ = policy switch
        {
            FutureWaitPolicy.FirtCompleted => new FirstCompletedPolicy<R>(futures),
            FutureWaitPolicy.AllCompleted => new AllCompletedPolicy<R>(futures),
            _ => throw new ArgumentOutOfRangeException()
        };
        return policy_.Wait();
    }
}

public class Future : Future<object>, ICompletableFuture {}
