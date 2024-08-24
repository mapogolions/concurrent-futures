namespace Futures.Internal;

internal interface ICompletableFuture<T> : ILockable
{
    T? GetResult();
    T? GetResult(TimeSpan timeout);
    T? GetResult(TimeSpan timeout, Action<ICompletableFuture<T>>? beforeWait);
    bool Cancel();
    bool Run();
    void SetResult(T result);
    void SetException(Exception exception);
    FutureState State { get; }
    bool Subscribe(IFutureAwaiter<T> awaiter);
    bool Unsubscribe(IFutureAwaiter<T> awaiter);
}


internal interface ICompletableFuture : ICompletableFuture<object> { }
