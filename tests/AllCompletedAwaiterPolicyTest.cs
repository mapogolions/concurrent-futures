using Futures.Internal;

namespace Futures.Tests;

public class AllCompletedAwaiterPolicyTest
{
    [Fact]
    public void ShouldWait_WhenSomeFuturesAreCompletedBeforeWait()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        future1.SetResult("foo");
        ICompletableFuture future2 = new Future();
        var policy = new AllCompletedPolicy<object>((Future)future1, (Future)future2);
        void beforeWait(IFutureAwaiterPolicy<object> _) => new Thread(() => future2.SetResult("bar")).Start();

        // Act
        var done = policy.Wait(Timeout.InfiniteTimeSpan, beforeWait);

        // Assert
        Assert.Equal(2, done.Count);
    }

    [Fact]
    public void ShouldWaitUntilAllFuturesAreCompletedWithResultOrException()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        var policy = new AllCompletedPolicy<object>((Future)future1, (Future)future2);

        void beforeWait(IFutureAwaiterPolicy<object> _)
        {
            new Thread(() => future1.SetResult("foo")).Start();
            new Thread(() => future2.SetException(new InvalidOperationException())).Start();
        }


        // Act
        var done = policy.Wait(Timeout.InfiniteTimeSpan, beforeWait);

        // Assert
        Assert.Equal(2, done.Count);
    }

    [Fact]
    public void ShouldAddCancelledFutureToCompletedCollection_OnlyWhenCancellationHasPropagated()
    {
        // Arrange
        ICompletableFuture future = new Future();
        future.Cancel();
        var policy = new AllCompletedPolicy<object>((Future)future);

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
        var done = Future.Wait(FutureWaitPolicy.AllCompleted, (Future)future1, (Future)future2);

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
        var done = Future.Wait(FutureWaitPolicy.AllCompleted, (Future)future1, (Future)future2);

        // Assert
        Assert.Equal(2, done.Count);
    }
}
