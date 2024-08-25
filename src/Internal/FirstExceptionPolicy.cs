namespace Futures.Internal;

internal sealed class FirstExceptionPolicy<T> : IFutureAwaiterPolicy<T>, IFutureAwaiter<T>
{
    private readonly AllCompletedPolicy<T> _policy;

    public FirstExceptionPolicy(params Future<T>[] futures)
    {
        _policy = new(futures, true);
    }

    void IFutureAwaiter<T>.AddCancellation(Future<T> future) => ((IFutureAwaiter<T>)_policy).AddCancellation(future);
    void IFutureAwaiter<T>.AddException(Future<T> future) => ((IFutureAwaiter<T>)_policy).AddException(future);
    void IFutureAwaiter<T>.AddResult(Future<T> future) => ((IFutureAwaiter<T>)_policy).AddResult(future);


    public IReadOnlyCollection<Future<T>> Wait() => _policy.Wait();

    public IReadOnlyCollection<Future<T>> Wait(TimeSpan timeout, Action<IFutureAwaiterPolicy<T>>? beforeWait = null) =>
        _policy.Wait(timeout, beforeWait);
}
