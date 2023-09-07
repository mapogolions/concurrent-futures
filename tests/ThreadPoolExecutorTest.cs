namespace Futures.Tests;

public class ThreadPoolExecutorTest
{
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
