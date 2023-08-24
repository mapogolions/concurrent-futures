namespace Futures;

public interface IFutureAwaiter<T>
{
    IReadOnlyCollection<Future<T>> Wait();
    IReadOnlyCollection<Future<T>> Wait(TimeSpan timeout);
}
