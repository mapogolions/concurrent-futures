namespace Futures.Awaiters;

public class FutureAwaiter
{
    internal readonly List<Future> Done = new();

    internal virtual void AddSuccess(Future future)
    {
        Done.Add(future);
    }

    internal virtual void AddFailure(Future future)
    {
        Done.Add(future);
    }

    internal virtual void AddCancellation(Future future)
    {
        Done.Add(future);
    }
}
