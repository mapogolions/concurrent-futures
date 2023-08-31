namespace Futures.Internal;

internal interface ICompletableFuture<T> : ILockable
{
    T? GetResult();
    T? GetResult(TimeSpan timeout);
    bool Cancel();
    void SetResult(T result);
    void SetException(Exception exception);
    FutureState State { get; }
    void AddPolicy(IFutureAwaiter<T> awaiter);
    void RemovePolicy(IFutureAwaiter<T> awaiter);
}

internal interface ICompletableFuture : ICompletableFuture<object> { }
