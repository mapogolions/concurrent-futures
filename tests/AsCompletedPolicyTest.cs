using Futures.Internal;

namespace Futures.Tests;

public class AsCompletedPolicyTest
{
    // [Fact]
    // public void ShouldReturnOnlyCompletedFutures_WhenTimeoutOccurs()
    // {
    //     // Arrange
    //     ICompletableFuture future1 = new Future();
    //     ICompletableFuture future2 = new Future();
    //     future2.SetResult("foo");
    //     var policy = new AllCompletedPolicy<object>((Future)future1, (Future)future2);

    //     // Act
    //     var done = policy.Wait(TimeSpan.FromMilliseconds(500));

    //     // Assert
    //     Assert.Single(done);
    // }

    // [Fact]
    // public void ShouldWait_WhenSomeFuturesAreCompletedBeforeWait()
    // {
    //     // Arrange
    //     ICompletableFuture future1 = new Future();
    //     future1.SetResult("foo");
    //     ICompletableFuture future2 = new Future();
    //     var policy = new AllCompletedPolicy<object>((Future)future1, (Future)future2);
    //     void beforeWait(IFutureAwaiterPolicy<object> _) => new Thread(() => future2.SetResult("bar")).Start();

    //     // Act
    //     var done = policy.Wait(Timeout.InfiniteTimeSpan, beforeWait);

    //     // Assert
    //     Assert.Equal(2, done.Count);
    // }

    // [Fact]
    // public void ShouldWaitUntilAllFuturesAreCompletedWithResultOrException()
    // {
    //     // Arrange
    //     ICompletableFuture future1 = new Future();
    //     ICompletableFuture future2 = new Future();
    //     var policy = new AllCompletedPolicy<object>((Future)future1, (Future)future2);

    //     void beforeWait(IFutureAwaiterPolicy<object> _)
    //     {
    //         new Thread(() => future1.SetResult("foo")).Start();
    //         new Thread(() => future2.SetException(new InvalidOperationException())).Start();
    //     }


    //     // Act
    //     var done = policy.Wait(Timeout.InfiniteTimeSpan, beforeWait);

    //     // Assert
    //     Assert.Equal(2, done.Count);
    // }

    // [Fact]
    // public void ShouldAddCancelledFutureToCompletedCollection_OnlyWhenCancellationHasPropagated()
    // {
    //     // Arrange
    //     ICompletableFuture future = new Future();
    //     future.Cancel();
    //     var policy = new AllCompletedPolicy<object>((Future)future);

    //     void beforeWait(IFutureAwaiterPolicy<object> _) => new Thread(() => future.Run()).Start();

    //     // Act
    //     var done = policy.Wait(Timeout.InfiniteTimeSpan, beforeWait);

    //     // Assert
    //     Assert.Single(done);
    // }

    [Fact]
    public void ShouldWaitCompletionOfAllFutures()
    {
        // Arrange
        var future1 = new Future<int>();
        var future2 = new Future<int>();

        new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromSeconds(4));
            ((ICompletableFuture<int>)future1).SetResult(1);
        }).Start();
        new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(50));
            ((ICompletableFuture<int>)future2).SetResult(2);
        }).Start();

        // Act
        var done = new List<Future<int>>();
        var iter = Future.AsCompleted(future1, future2);
        foreach (var future in iter)
        {
            done.Add(future);
        }

        // Assert
        Assert.Equal(2, done.Count);
        Assert.Equal(2, done[0].GetResult());
        Assert.Equal(1, done[1].GetResult());
    }

    [Fact]
    public void ShouldAddFuturesToCompletedCollection_WhenThreyAreCompletedWithExceptionBeforeWait()
    {
        // Arrange
        var future1 = new Future();
        var future2 = new Future();
        ((ICompletableFuture)future1).SetException(new InvalidOperationException());
        ((ICompletableFuture)future2).SetException(new InvalidOperationException());

        // Act
        var policy = new AsCompletedPolicy<object>(future1, future2);
        var done = policy.Wait();

        // Assert
        Assert.Equal(2, done.Count);
    }

    [Fact]
    public void ShouldAddFuturesToCompletedCollection_WhenTheyAreCompletedWithResultBeforeWait()
    {
        // Arrange
        var future1 = new Future<string>();
        var future2 = new Future<string>();
        ((ICompletableFuture<string>)future1).SetResult("foo");
        ((ICompletableFuture<string>)future2).SetResult("bar");

        // Act
        var policy =   new AsCompletedPolicy<string>(future1, future2);
        var done = policy.Wait();

        // Assert
        Assert.Equal(2, done.Count);
    }
}
