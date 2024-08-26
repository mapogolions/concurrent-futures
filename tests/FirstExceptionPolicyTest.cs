using Futures.Internal;

namespace Futures.Tests;

public class FirstExceptionPolicyTest
{
    [Fact]
    public void ShouldReturnOnlyCompletedFutures_WhenTimeoutOccurs()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        future2.SetResult("foo");
        var policy = new FirstExceptionPolicy<object>((Future)future1, (Future)future2);

        // Act
        var done = policy.Wait(TimeSpan.FromMilliseconds(500));

        // Assert
        Assert.Single(done);
    }

    [Fact]
    public void ShouldWaitAllFuturesWhenThereIsNoCompletionWithException()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        var policy = new FirstExceptionPolicy<object>((Future)future1, (Future)future2);

        void beforeWait(IFutureAwaiterPolicy<object> _)
        {
            new Thread(() =>
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(200));
                future1.SetResult("foo");
            }).Start();

            new Thread(() =>
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(300));
                future2.SetResult("bar");
            }).Start();
        }

        // Act
        var done = policy.Wait(Timeout.InfiniteTimeSpan, beforeWait);

        // Assert
        Assert.Equal(2, done.Count);
    }

    [Fact]
    public void ShouldWaitUntilFirstCompletionWithExceptionOccurs()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        var policy = new FirstExceptionPolicy<object>((Future)future1, (Future)future2);

        void beforeWait(IFutureAwaiterPolicy<object> _)
        {
            new Thread(() =>
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(200));
                future2.SetException(new InvalidOperationException());
            }).Start();
        }

        // Act
        var done = policy.Wait(Timeout.InfiniteTimeSpan, beforeWait);

        // Assert
        Assert.Single(done);
    }

    [Fact]
    public void ShouldAddCancelledFutureToCompletedCollection_OnlyWhenCancellationHasPropagated()
    {
        // Arrange
        ICompletableFuture future = new Future();
        future.Cancel();
        var policy = new FirstExceptionPolicy<object>((Future)future);

        void beforeWait(IFutureAwaiterPolicy<object> _) => new Thread(() => future.Run()).Start();

        // Act
        var done = policy.Wait(Timeout.InfiniteTimeSpan, beforeWait);

        // Assert
        Assert.Single(done);
    }

    [Fact]
    public void ShouldAddFuturesToCompletedCollection_WhenThreyAreCompletedWithExceptionBeforeWait()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        future1.SetException(new InvalidOperationException());
        future2.SetException(new InvalidOperationException());

        // Act
        var done = Future.Wait(FutureWaitPolicy.FirstException, (Future)future1, (Future)future2);

        // Assert
        Assert.Equal(2, done.Count);
    }

    [Fact]
    public void ShouldAddFuturesToCompletedCollection_WhenTheyAreCompletedWithResultBeforeWait()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        future1.SetResult("foo");
        future2.SetResult("bar");

        // Act
        var done = Future.Wait(FutureWaitPolicy.FirstException, (Future)future1, (Future)future2);

        // Assert
        Assert.Equal(2, done.Count);
    }
}
