namespace Futures.Internal;

internal interface IFutureAwaiter<T>
{
    IReadOnlyCollection<Future<T>> Wait();
    IReadOnlyCollection<Future<T>> Wait(TimeSpan timeout);
}
