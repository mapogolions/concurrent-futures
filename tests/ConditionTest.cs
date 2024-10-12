using Futures.Internal;
using System.Diagnostics;

namespace Futures.Tests;

public class ConditionTest
{
    [Fact]
    public void ShouldIgnoreSpuriousSignalAndExitOnTimeout()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(2);
        var cond = new Condition();
        cond.Acquire();

        var t = new Thread(() =>
        {
            cond.Acquire();
            cond.SpuriousSignal();
            cond.Release();
        });
        t.Start();

        // Act + Assert
        Assert.False(cond.Wait(timeout));
        cond.Release();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(200)]
    public void ShouldExitOnTimeout(int ms)
    {
        var cond = new Condition();
        cond.Acquire();
        var signaled = cond.Wait(TimeSpan.FromMilliseconds(ms));
        cond.Release();
        Assert.False(signaled);
    }

    [Fact]
    public void ShouldNotify()
    {
        // Arrange
        var cond = new Condition();
        var acquired = new ManualResetEventSlim(false);
        var t = new Thread(() =>
        {
            cond.Acquire();
            acquired.Set();
            cond.Wait(Timeout.InfiniteTimeSpan);
            cond.Release();
        });

        // Act + Assert
        t.Start();
        acquired.Wait();
        
        cond.Acquire();
        cond.NotifyOne();
        cond.Release();

        t.Join();

        Assert.True(true);
    }
}