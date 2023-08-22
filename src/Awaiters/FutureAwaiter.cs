namespace Futures.Awaiters;

public class FutureAwaiter
{
    protected readonly List<Future<object>> Done = new();

    protected virtual void AddSuccess(Future<object> future)
    {
        Done.Add(future);
    }

    protected virtual void AddFailure(Future<object> future)
    {
        Done.Add(future);
    }

    protected virtual void AddCancellation(Future<object> future)
    {
        Done.Add(future);
    }
}
