namespace Futures;

public interface IFuture<T>
{
    T? GetResult();
    T? GetResult(TimeSpan timeout);
    bool Cancel();
}
