using Futures.Awaiters;

namespace Futures.Internals;

internal interface ICompletableFuture : ICompletableFuture<object>
{
}


internal interface ICompletableFuture<T> : IFuture<T>, ILockable
{
    void SetResult(T result);
    void SetException(Exception exception);
    FutureState State { get; }
    void SubscribeUnsafe(FutureAwaiter<T> awaiter);
    void UnsubscribeUnsafe(FutureAwaiter<T> awaiter);
}
