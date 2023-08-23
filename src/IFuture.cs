namespace Futures;


public interface IFuture<out T>
{
    T? GetResult();
    T? GetResult(TimeSpan timeout);
    bool Cancel();
}

public interface IFuture : IFuture<object>
{
}
