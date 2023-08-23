using Futures.Awaiters;
using Futures.Internals;

namespace Futures.Tests;


public class FirstCompletedFuturePolicyTest
{
    [Fact]
    public void ShouldWaitFutureUntilItCompletedWithException()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        var awaiter = new FirstCompletedFuturePolicy((Future)future1, (Future)future2);
        var t = new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(50));
            future1.SetException(new InvalidOperationException());
        });

        // Act
        t.Start();
        var done = awaiter.Wait();

        // Arrange
        Assert.Single(done);

    }

    [Fact]
    public void ShouldWaitFutureUntilItCompletedWithResult()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        var awaiter = new FirstCompletedFuturePolicy((Future)future1, (Future)future2);
        var t = new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(1));
            future1.SetResult("foo");
        });

        // Act
        t.Start();
        var done = awaiter.Wait();

        // Arrange
        Assert.Single(done);
    }

    [Fact]
    public void ShouldAddFutureToCompletedCollection_WhenItHasBeenCancelledBeforeWait()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        future1.Cancel();
        var awaiter = new FirstCompletedFuturePolicy((Future)future1, (Future)future2);

        // Act
        var done = awaiter.Wait();

        // Assert
        Assert.Single(done);
    }

    [Fact]
    public void ShouldAddFutureToCompletedCollection_WhenItCompletedWithExceptionBeforeWait()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        future1.SetException(new InvalidOperationException());
        var awaiter = new FirstCompletedFuturePolicy((Future)future1, (Future)future2);

        // Act
        var done = awaiter.Wait();

        // Assert
        Assert.Single(done);
    }

    [Fact]
    public void ShouldAddFutureToCompletedCollection_WhenItIsAlreadyCompletedWithResultBeforeWait()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        future1.SetResult("foo");
        var awaiter = new FirstCompletedFuturePolicy((Future)future1, (Future)future2);

        // Act
        var done = awaiter.Wait();

        // Assert
        Assert.Single(done);
    }
}
