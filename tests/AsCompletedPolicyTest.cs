using Futures.Internal;

namespace Futures.Tests;

public class AsCompletedPolicyTest
{
    [Fact]
    public void ShouldIterate()
    {
        // Arrange
        var future1 = new Future<int>();
        var future2 = new Future<int>();

        new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(500));
            ((ICompletableFuture<int>)future1).SetResult(1);
        }).Start();
        new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(10));
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
