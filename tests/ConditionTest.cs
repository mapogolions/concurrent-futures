using System.Diagnostics;
using Future.Internal;
using Futures.Internal;

namespace Futures.Tests;

public class ConditionTest
{
    [Fact]
    public void ShouldIgnoreSpuriousSignalAndExitOnTimeout()
    {
        // Arrange
        var (clock, cond) = (new Stopwatch(), new Condition());
        var (timeout, tolerance) = (TimeSpan.FromSeconds(2), Monotonic.Ticks(TimeSpan.FromMilliseconds(20)));
        
        // Act
        cond.Acquire();
        new Thread(() =>
        {
            cond.Acquire();
            cond.SpuriousSignal();
            cond.Release();
        }).Start();

        clock.Start();
        var signaled = cond.Wait(timeout);
        clock.Stop();
        cond.Release();
       
        // Assert
        Assert.False(signaled);
        Assert.True(clock.ElapsedTicks + tolerance >= Monotonic.Ticks(timeout));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(200)]
    public void ShouldExitOnTimeout(int ms)
    {
        // Arrange
        var cond = new Condition();
        
        // Act
        cond.Acquire();
        var signaled = cond.Wait(TimeSpan.FromMilliseconds(ms));
        cond.Release();
        
        // Assert
        Assert.False(signaled);
    }

    [Fact]
    public void ShouldInterruptWait()
    {
        // Arrange
        var timeout = TimeSpan.FromHours(1);
        var (clock, cond) = (new Stopwatch(), new Condition());
        
        // Act
        cond.Acquire();
        new Thread(() =>
        {
            cond.Acquire();
            cond.NotifyOne();
            cond.Release();
        }).Start();

        clock.Start();
        var signaled = cond.Wait(timeout);
        clock.Stop();
        cond.Release();
        
        // Assert
        Assert.True(signaled);
        Assert.True(clock.ElapsedTicks < Monotonic.Ticks(timeout));
    }

    [Fact]
    public void ShouldInterruptInfiniteWait()
    {
        // Arrange
        var cond = new Condition();

        // Act
        cond.Acquire();
        new Thread(() =>
        {
            cond.Acquire();
            cond.NotifyOne();
            cond.Release();
        }).Start();

        var signaled = cond.Wait(Timeout.InfiniteTimeSpan);
        cond.Release();
        
        // Assert
        Assert.True(signaled);
    }
}