using Futures.Internal;
using System.Diagnostics;

namespace Futures.Tests;

public class ConditionTest
{
    [Fact]
    public void ShouldIgnoreSpuriousSignalAndExitOnTimeout()
    {
        // Arrange
        var clock = new Stopwatch();
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
        clock.Start();
        Assert.False(cond.Wait(timeout));
        clock.Stop();
        Assert.True(clock.ElapsedTicks >= timeout.Ticks);
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
    public void ShouldInterruptWait()
    {
        var clock = new Stopwatch();
        var timeout = TimeSpan.FromHours(1);
        var cond = new Condition();
        cond.Acquire();

        new Thread(() =>
        {
            cond.Acquire();
            cond.NotifyOne();
            cond.Release();
        }).Start();

        clock.Start();
        Assert.True(cond.Wait(timeout));
        clock.Stop();
        Assert.True(clock.ElapsedTicks < timeout.Ticks);
        cond.Release();
    }

    [Fact]
    public void ShouldInterruptInfiniteWait()
    {
        var cond = new Condition();
        cond.Acquire();

        new Thread(() =>
        {
            cond.Acquire();
            cond.NotifyOne();
            cond.Release();
        }).Start();

        Assert.True(cond.Wait(Timeout.InfiniteTimeSpan));
        cond.Release();
    }
}