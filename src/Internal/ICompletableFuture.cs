namespace Futures.Internal;

internal interface ICompletableFuture<T> : ILockable
{
    T? GetResult();
    T? GetResult(TimeSpan timeout);
    bool Cancel();
    void SetResult(T result);
    void SetException(Exception exception);
    FutureState State { get; }
    void AddPolicy(IFutureAwaiterPolicy<T> policy);
    void RemovePolicy(IFutureAwaiterPolicy<T> policy);
}

internal interface ICompletableFuture : ICompletableFuture<object> { }
