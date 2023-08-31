using Futures.Internal;

namespace Futures;


internal sealed class WorkItem<T>
{
    public WorkItem(ICompletableFuture<T> future, Func<object?, T> callback, object? state)
    {
        Future = future;
        Callback = callback;
        State = state;
    }

    public ICompletableFuture<T> Future { get; }
    public Func<object?, T> Callback { get; }
    public object? State { get; }

    public void Run()
    {
        // set running state
        try
        {
            var result = Callback(State);
            Future.SetResult(result);
        }
        catch (Exception ex)
        {
            Future.SetException(ex);
        }
    }
}

public class ThreadPoolExecutor
{
    private readonly int _maxWorkers;
    private bool _shoutdown;
}
