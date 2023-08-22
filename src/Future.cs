namespace Futures;

using Futures.Awaiters;
using Futures.Internals;


public class Future<T> : ILockable
{
    private T? _result;
    private Exception? _exception;
    private readonly Mutex _mutex = new();
    private readonly List<FutureAwaiter> _awaiters = new();

    public T? GetResult() => GetResult(Timeout.InfiniteTimeSpan);

    public T? GetResult(TimeSpan timeout)
    {
        Monitor.Enter(_mutex);
        if (State is FutureState.Cancelled)
        {
            throw new CancelledFutureException();
        }
        if (State is FutureState.Completed)
        {
            var result = ResultUnsafe();
            Monitor.Exit(_mutex);
            return result;
        }

        // Pending or Running => wait
        Monitor.Wait(_mutex, timeout);

        if (State is FutureState.Cancelled)
        {
            throw new CancelledFutureException();
        }
        if (State is FutureState.Completed)
        {
            var result = ResultUnsafe();
            Monitor.Exit(_mutex);
            return result;
        }
        throw new TimeoutException();
    }

    internal FutureState State { get; set; } = FutureState.Pending;
    internal void SubscribeUnsafe(FutureAwaiter awaiter) => _awaiters.Add(awaiter);
    internal void UnsubscribeUnsafe(FutureAwaiter awaiter) => _awaiters.Remove(awaiter);

    internal void SetResult(T result)
    {
        Monitor.Enter(_mutex);
        if (State is FutureState.Cancelled || State is FutureState.Completed)
        {
            throw new InvalidFutureStateException();
        }
        _result = result;
        State = FutureState.Completed;
        Monitor.PulseAll(_mutex);
        Monitor.Exit(_mutex);
    }

    internal void SetException(Exception exception)
    {
        Monitor.Enter(_mutex);
        if (State is FutureState.Cancelled || State is FutureState.Completed)
        {
            throw new InvalidFutureStateException();
        }
        _exception = exception;
        State = FutureState.Completed;
        Monitor.PulseAll(_mutex);
        Monitor.Exit(_mutex);
    }

    private T? ResultUnsafe()
    {
        if (_exception is not null)
        {
            throw _exception;
        }
        return _result;
    }

    public bool Cancel()
    {
        Monitor.Enter(_mutex);
        if (State is FutureState.Pending)
        {
            State = FutureState.Cancelled;
            Monitor.PulseAll(_mutex);
            Monitor.Exit(_mutex);
            return true;
        }
        var state = State;
        Monitor.Exit(_mutex);
        return state is FutureState.Cancelled;
    }

    public void Acquire()
    {
        throw new NotImplementedException();
    }

    public void Release()
    {
        throw new NotImplementedException();
    }
}

public enum FutureState
{
    Pending,
    Running,
    Cancelled,
    Completed
}
