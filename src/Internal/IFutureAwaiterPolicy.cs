namespace Futures.Internal;

internal interface IFutureAwaiterPolicy<T>
{
    IReadOnlyCollection<Future<T>> Wait();
    IReadOnlyCollection<Future<T>> Wait(TimeSpan timeout);
}
