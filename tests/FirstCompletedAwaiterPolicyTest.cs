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
        var t = new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(50));
            future1.SetException(new InvalidOperationException());
        });

        // Act
        t.Start();
        var done = Future.Wait(FutureWaitPolicy.FirtCompleted, (Future)future1, (Future)future2);

        // Arrange
        Assert.Single(done);

    }

    [Fact]
    public void ShouldWaitFutureUntilItIsCompletedWithResult()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        var t = new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(50));
            future1.SetResult("foo");
        });

        // Act
        t.Start();
        var done = Future.Wait(FutureWaitPolicy.FirtCompleted, (Future)future1, (Future)future2);

        // Arrange
        Assert.Single(done);
    }

    [Fact]
    public void ShouldNotAddFutureToCompletedCollection_WhenItIsCancelledBeforeWait()
    {
        // Arrange
        ICompletableFuture future = new Future();
        future.Cancel();
        var t = new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(10));
            future.Run();
        });
        t.Start();

        // Act
        var done = Future.Wait(FutureWaitPolicy.FirtCompleted, (Future)future);

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
        var done = Future.Wait(FutureWaitPolicy.FirtCompleted, (Future)future1, (Future)future2);

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
        var done = Future.Wait(FutureWaitPolicy.FirtCompleted, (Future)future1, (Future)future2);

        // Assert
        Assert.Single(done);
    }
}
