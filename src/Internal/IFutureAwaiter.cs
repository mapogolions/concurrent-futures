namespace Futures.Internal;

internal interface IFutureAwaiter<T>
{
    void AddResult(Future<T> future);
    void AddException(Future<T> future);
    void AddCancellation(Future<T> future);
}
