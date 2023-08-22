namespace Future.Tests;

using Futures;

public class FutureTest
{
    [Fact]
    public void ShouldThrowException_WhenSetExceptionOnCancelledFuture()
    {
        var future = new Future<string>();
        future.Cancel();
        Assert.Throws<InvalidFutureStateException>(() => future.SetException(new InvalidOperationException()));
    }

    [Fact]
    public void ShouldThrowException_WhenSetExceptionOnCompletedFuture()
    {
        var future = new Future<string>();
        future.SetResult("foo");
        Assert.Throws<InvalidFutureStateException>(() => future.SetException(new InvalidOperationException()));
    }

    [Fact]
    public void ShouldThrowException_WhenSetResultOnCancelledFuture()
    {
        var future = new Future<string>();
        future.Cancel();
        Assert.Throws<InvalidFutureStateException>(() => future.SetResult("foo"));
    }

    [Fact]
    public void ShouldThrowException_WhenSetResultOnCompletedFuture()
    {
        var future = new Future<string>();
        future.SetResult("foo");
        Assert.Throws<InvalidFutureStateException>(() => future.SetResult("foo"));
    }

    [Fact]
    public void ShouldThrowException_WhenGetResultOnCompletedFutureWithException()
    {
        // Arrange
        var future = new Future<object>();
        var t = new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(10));
            future.SetException(new InvalidOperationException());
        });
        t.Start();

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => future.GetResult());
    }

    [Fact]
    public void ShouldReturnResult_WhenGetResultOnCompletedFutureWithValue()
    {
        // Arrange
        var future = new Future<string>();
        var t = new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(10));
            future.SetResult("foo");
        });
        t.Start();

        // Act
        var result = future.GetResult();

        // Assert
        Assert.Equal("foo", result);
    }

    [Fact]
    public void GetResultShouldThrowTimeoutException()
    {
        // Arrange
        var future = new Future<object>();
        var start = DateTime.UtcNow;
        var timeout = TimeSpan.FromMilliseconds(100);

        // Act + Assert
        Assert.Throws<TimeoutException>(() => future.GetResult(timeout));
    }

    [Fact]
    public void CancellationShouldReturnTrue_WhenFutureHasBeenAlreadyCancelled()
    {
        var future = new Future<object>();
        future.Cancel();
        Assert.True(future.Cancel());
    }

    [Fact]
    public void ShouldCancelPendingFuture()
    {
        var future = new Future<object>();
        Assert.True(future.Cancel());
    }
}
