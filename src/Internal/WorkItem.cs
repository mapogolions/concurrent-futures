namespace Futures.Internal;

internal sealed class WorkItem<T>(ICompletableFuture<T> future, Func<object?, T> callback, object? state)
{
    public void Run()
    {
        if (!future.Run()) return;
        try
        {
            var result = callback(state);
            future.SetResult(result);
        }
        catch (Exception ex)
        {
            future.SetException(ex);
        }
    }
}
