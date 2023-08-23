using Futures.Awaiters;
using Futures.Internals;

namespace Futures.Tests;


public class FirstCompletedFutureAwaiterTest
{
    [Fact]
    public void ShouldWaitFutureUntilComplete()
    {
        // Arrange
        ICompletableFuture future = new Future();
        var awaiter = new FirstCompletedFutureAwaiter((Future)future);
        var t = new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(1));
            future.SetResult("foo");
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
        ICompletableFuture future = new Future();
        future.Cancel();
        var awaiter = new FirstCompletedFutureAwaiter((Future)future);

        // Act
        var done = awaiter.Wait();

        // Assert
        Assert.Single(done);
    }

    [Fact]
    public void ShouldAddFutureToCompletedCollection_WhenItCompletedWithExceptionBeforeWait()
    {
        // Arrange
        ICompletableFuture future = new Future();
        future.SetException(new InvalidOperationException());
        var awaiter = new FirstCompletedFutureAwaiter((Future)future);

        // Act
        var done = awaiter.Wait();

        // Assert
        Assert.Single(done);
    }

    [Fact]
    public void ShouldAddFutureToCompletedCollection_WhenItIsAlreadyCompletedWithResultBeforeWait()
    {
        // Arrange
        ICompletableFuture future = new Future();
        future.SetResult("foo");
        var awaiter = new FirstCompletedFutureAwaiter((Future)future);

        // Act
        var done = awaiter.Wait();

        // Assert
        Assert.Single(done);
    }
}
