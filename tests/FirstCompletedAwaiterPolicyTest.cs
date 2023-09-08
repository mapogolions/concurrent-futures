using Futures.Internal;

namespace Futures.Tests;

public class FirstCompletedAwaiterPolicyTest
{
    [Fact]
    public void ShouldWaitFutureUntilItIsCompletedWithException()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        var policy = new FirstCompletedPolicy<object>((Future)future1, (Future)future2);

        void beforeWait(IFutureAwaiterPolicy<object> _) =>
            new Thread(() => future1.SetException(new InvalidOperationException())).Start();

        // Act
        var done = policy.Wait(Timeout.InfiniteTimeSpan, beforeWait);

        // Arrange
        Assert.Single(done);

    }

    [Fact]
    public void ShouldWaitFutureUntilItIsCompletedWithResult()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        var policy = new FirstCompletedPolicy<object>((Future)future1, (Future)future2);
        void beforeWait(IFutureAwaiterPolicy<object> _) => new Thread(() => future1.SetResult("foo")).Start();

        // Act
        var done = policy.Wait(Timeout.InfiniteTimeSpan, beforeWait);

        // Arrange
        Assert.Single(done);
    }

    [Fact]
    public void ShouldAddCancelledFutureToCompletedCollection_OnlyWhenCancellationHasPropagated()
    {
        // Arrange
        ICompletableFuture future = new Future();
        future.Cancel();
        var policy = new FirstCompletedPolicy<object>((Future)future);
        void beforeWait(IFutureAwaiterPolicy<object> _) => new Thread(() => future.Run()).Start();

        // Act
        var done = policy.Wait(Timeout.InfiniteTimeSpan, beforeWait);

        // Assert
        Assert.Single(done);
    }

    [Fact]
    public void ShouldAddFutureToCompletedCollection_WhenItIsCompletedWithExceptionBeforeWait()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        future1.SetException(new InvalidOperationException());

        // Act
        var done = Future.Wait(FutureWaitPolicy.FirstCompleted, (Future)future1, (Future)future2);

        // Assert
        Assert.Single(done);
    }

    [Fact]
    public void ShouldAddFutureToCompletedCollection_WhenItIsCompletedWithResultBeforeWait()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        future1.SetResult("foo");

        // Act
        var done = Future.Wait(FutureWaitPolicy.FirstCompleted, (Future)future1, (Future)future2);

        // Assert
        Assert.Single(done);
    }
}
