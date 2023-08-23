namespace Futures;

public interface IFuture
{
    object? GetResult();
    object? GetResult(TimeSpan timeout);
    bool Cancel();
}
