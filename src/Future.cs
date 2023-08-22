namespace Futures;


class CancelledFutureException : Exception  {}

public class Future<T>
{
    private T? _result;
    private Exception? _exception;
    private FutureState _state = FutureState.Pending;
    private readonly Mutex _mutex = new();


    public T Result(TimeSpan timeout)
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

    private T ResultUnsafe()
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
        if (_state is FutureState.Pending)
        {
            _state = FutureState.Cancelled;
            Monitor.PulseAll(_mutex);
            Monitor.Exit(_mutex);
            // call callbacks
            return true;
        }
        var state = _state;
        Monitor.Exit(_mutex);
        return state is FutureState.Cancelled;
    }

    public bool Call()
    {
        return true;
    }
}

public enum FutureState
{
    Pending,
    Running,
    Cancelled,
    Finished
}
