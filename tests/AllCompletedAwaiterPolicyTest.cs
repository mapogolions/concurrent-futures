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

        var t = new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(50));
            future2.SetResult("bar");
        });

        // Act
        t.Start();
        var done = Future.Wait(FutureWaitPolicy.AllCompleted, (Future)future1, (Future)future2);

        // Assert
        Assert.Equal(2, done.Count);
    }

    [Fact]
    public void ShouldWaitUntilAllFuturesAreCompletedWithResultOrException()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        var t1 = new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            future1.SetResult("foo");
        });
        var t2 = new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            future2.SetException(new InvalidOperationException());
        });


        // Act
        t1.Start();
        t2.Start();
        var done = Future.Wait(FutureWaitPolicy.AllCompleted, (Future)future1, (Future)future2);

        // Assert
        Assert.Equal(2, done.Count);
    }

    [Fact]
    public void ShouldNotAddFuturesToCompletedCollection_WhenTheyAreCancelledBeforeWait()
    {
        // Arrange
        ICompletableFuture future = new Future();
        future.Cancel();
        var t = new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(50));
            future.Run();
        });

        // Act
        t.Start();
        var done = Future.Wait(FutureWaitPolicy.FirtCompleted, (Future)future);

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
        var done = Future.Wait(FutureWaitPolicy.FirtCompleted, (Future)future1, (Future)future2);

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
