namespace Futures.Internal;

internal interface ICompletableFuture<T> : ILockable
{
    T? GetResult();
    T? GetResult(TimeSpan timeout);
    bool Cancel();
    bool Run();
    void SetResult(T result);
    void SetException(Exception exception);
    FutureState State { get; }
    void Subscribe(IFutureAwaiter<T> awaiter);
    void Unsubscribe(IFutureAwaiter<T> awaiter);
}

internal interface ICompletableFuture : ICompletableFuture<object> { }
