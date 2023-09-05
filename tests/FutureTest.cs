namespace Futures.Tests;

using Futures;
using Futures.Internal;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;

public class FutureTest
{
    [Fact]
    public void ShouldThrowException_WhenSetExceptionOnCancelledFuture()
    {
        ICompletableFuture future = new Future();
        future.Cancel();
        Assert.Throws<InvalidFutureStateException>(() => future.SetException(new InvalidOperationException()));
    }

    [Fact]
    public void ShouldThrowException_WhenSetExceptionOnCompletedFuture()
    {
        ICompletableFuture future = new Future();
        future.SetResult("foo");
        Assert.Throws<InvalidFutureStateException>(() => future.SetException(new InvalidOperationException()));
    }

    [Fact]
    public void ShouldThrowException_WhenSetResultOnCancelledFuture()
    {
        ICompletableFuture future = new Future();
        future.Cancel();
        Assert.Throws<InvalidFutureStateException>(() => future.SetResult("foo"));
    }

    [Fact]
    public void ShouldThrowException_WhenSetResultOnCompletedFuture()
    {
        ICompletableFuture future = new Future();
        future.SetResult("foo");
        Assert.Throws<InvalidFutureStateException>(() => future.SetResult("foo"));
    }

    [Fact]
    public void ShouldThrowException_WhenGetResultOnCompletedFutureWithException()
    {
        // Arrange
        ICompletableFuture future = new Future();
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
        ICompletableFuture future = new Future();
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
        var future = new Future();
        var start = DateTime.UtcNow;
        var timeout = TimeSpan.FromMilliseconds(100);

        // Act + Assert
        Assert.Throws<TimeoutException>(() => future.GetResult(timeout));
    }

    [Fact]
    public void ShouldThrowException_WhenTryingToRunFutureThatHasAlreadyBeenRun()
    {
        ICompletableFuture future = new Future();
        future.Run();
        Assert.Throws<InvalidFutureStateException>(() => future.Run());
    }

    [Fact]
    public void ShouldNotBeAbleToRunCancelledFuture()
    {
        ICompletableFuture future = new Future();
        future.Cancel();
        Assert.False(future.Run());
        Assert.Equal(FutureState.CancellationPropagated, future.State);
    }

    [Fact]
    public void ShouldSuccessfullyRunPendingFuture()
    {
        ICompletableFuture future = new Future();
        Assert.Equal(FutureState.Pending, future.State);
        Assert.True(future.Run());
        Assert.Equal(FutureState.Running, future.State);
    }

    [Fact]
    public void CancellationShouldReturnTrue_WhenFutureCancelledAndCancellationPropagated()
    {
        ICompletableFuture future = new Future();
        future.Cancel();
        future.Run();

        Assert.Equal(FutureState.CancellationPropagated, future.State);
        Assert.True(future.Cancel());
    }

    [Fact]
    public void CancellationShouldReturnTrue_WhenFutureCancelled()
    {
        ICompletableFuture future = new Future();
        future.Cancel();

        Assert.Equal(FutureState.Cancelled, future.State);
        Assert.True(future.Cancel());
    }

    [Fact]
    public void ShouldCancelPendingFuture()
    {
        var future = new Future();
        Assert.True(future.Cancel());
    }
}
