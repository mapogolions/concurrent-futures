namespace Futures.Internal;

internal sealed class WorkItem<T>
{
    private readonly ICompletableFuture<T> _future;
    private readonly Func<object?, T> _callback;
    private readonly object? _state;

    public WorkItem(ICompletableFuture<T> future, Func<object?, T> callback, object? state)
    {
        _future = future;
        _callback = callback;
        _state = state;
    }

    public void Run()
    {
        _future.Run();
        try
        {
            var result = _callback(_state);
            _future.SetResult(result);
        }
        catch (Exception ex)
        {
            _future.SetException(ex);
        }
    }
}
