namespace Futures.Tests;

public class ThreadPoolExecutorTest
{
    [Fact]
    public void ShouldPropagateCancellation_WhenFutureHasBeenCancelled()
    {
        // Arrange
        var executor = new ThreadPoolExecutor(1);
        var signal = new ManualResetEvent(false);
        var f1 = executor.Submit<string>(s =>
        {
            // In single thread, this prevents execution of the second future until we cancel it
            signal.WaitOne();
            return (string)s!;
        }, "foo");
        var f2 = executor.Submit<string>(s => (string)s!, "bar");

        // Act + Assert
        f2.Cancel();
        signal.Set();
        Assert.Throws<CancelledFutureException>(() => f2.GetResult());
    }

    [Fact]
    public void ShouldFulfillFutureUsingThreadPoolExecutor()
    {
        // Arrange
        var executor = new ThreadPoolExecutor(2);
        var future = executor.Submit<string>(s =>
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            return (string)s!;
        }, "foo");

        // Act + Assert
        Assert.Equal("foo", future.GetResult());
    }
}
