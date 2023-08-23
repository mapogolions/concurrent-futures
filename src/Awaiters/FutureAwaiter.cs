namespace Futures.Awaiters;

public class FutureAwaiter<T>
{
    internal readonly List<Future<T>> Done = new();

    internal virtual void AddSuccess(Future<T> future)
    {
        Done.Add(future);
    }

    internal virtual void AddFailure(Future<T> future)
    {
        Done.Add(future);
    }

    internal virtual void AddCancellation(Future<T> future)
    {
        Done.Add(future);
    }
}
