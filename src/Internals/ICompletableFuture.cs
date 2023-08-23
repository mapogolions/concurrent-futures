using Futures.Awaiters;

namespace Futures.Internals;

internal interface ICompletableFuture : IFuture, ILockable
{
    void SetResult(object result);
    void SetException(Exception exception);
    FutureState State { get; }
    void SubscribeUnsafe(FutureAwaiter awaiter);
    void UnsubscribeUnsafe(FutureAwaiter awaiter);
}
