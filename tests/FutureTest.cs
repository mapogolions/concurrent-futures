namespace Futures.Tests;

using Futures;
using Futures.Internal;

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
    public void GetResultShouldReleaseLockAfterThrowingCancelledFutureException()
    {
        // Arrange
        ICompletableFuture future = new Future();
        future.Cancel();

        // Act + Assert
        Assert.Throws<CancelledFutureException>(() => future.GetResult());
        var mre = new ManualResetEvent(false);
        new Thread(() =>
        {
            Assert.Throws<CancelledFutureException>(() => future.GetResult());
            mre.Set();
        }).Start();
        mre.WaitOne();
    }

    [Fact]
    public void GetResultShouldReleaseLockAfterThrowingExceptionThatOccursDuringExecution()
    {
        // Arrange
        ICompletableFuture future = new Future();
        future.SetException(new InvalidOperationException());

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => future.GetResult());

        var mre = new ManualResetEvent(false);
        new Thread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => future.GetResult());
            mre.Set();
        }).Start();
        mre.WaitOne();
    }

    [Fact]
    public void ShouldBeAbleToGetResultInDifferentThreads_WhenFutureCompletedWithValue()
    {
        // Arrange
        ICompletableFuture future = new Future();
        future.SetResult(true);

        // Act + Assert
        Assert.Equal(true, future.GetResult());

        var mre = new ManualResetEvent(false);
        new Thread(() =>
        {
            Assert.Equal(true, future.GetResult());
            mre.Set();
        }).Start();
        mre.WaitOne();
    }

    [Fact]
    public void GetResultShouldThrowException_WhenFutureHasBeenCancelled()
    {

        ICompletableFuture future = new Future();
        future.Cancel();
        Assert.Throws<CancelledFutureException>(() => future.GetResult());
    }

    [Fact]
    public void GetResultShouldThrowException_WheFutureCompletedWithException()
    {
        // Arrange
        ICompletableFuture future = new Future();
        static void beforeWait(ICompletableFuture<object> future) =>
            new Thread(() => future.SetException(new InvalidOperationException())).Start();

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => future.GetResult(Timeout.InfiniteTimeSpan, beforeWait));
    }

    [Fact]
    public void GetResultShouldReturnResult_WhenFutureCompletedWithValue()
    {
        // Arrange
        ICompletableFuture future = new Future();
        static void beforeWait(ICompletableFuture<object> future) =>
            new Thread(() => future.SetResult("foo")).Start();

        // Act
        var result = future.GetResult(Timeout.InfiniteTimeSpan, beforeWait);

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
    public void ShouldNotBeAbleToRunCancelledFutureAndPropagateCancellation()
    {
        ICompletableFuture future = new Future();
        future.Cancel();
        Assert.Equal(FutureState.Cancelled, future.State);
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
        future.Run(); // propagate cancellation

        Assert.Equal(FutureState.CancellationPropagated, future.State);
        Assert.True(future.Cancel());
    }

    [Fact]
    public void CancellationShouldReturnTrue_WhenFutureCancelled() // Idempotancy
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
