using Future.Internal;
using System.Diagnostics;

namespace Futures.Internal;

internal sealed class Condition
{
    private readonly object _lock = new();
    private bool _flag;

    public void Acquire()
    {
        Monitor.Enter(_lock);
    }

    // Must be called under the lock, i.e. after `Condition.Acquire`
    public bool Wait(TimeSpan timeout)
    {
        if (timeout == Timeout.InfiniteTimeSpan)
        {
            return WaitInfinite();
        }
        if (timeout < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }
        return WaitTimeout(timeout);
    }

    private bool WaitInfinite()
    {
        while (!Volatile.Read(ref _flag))
        {
            Monitor.Wait(_lock);
        }
        return true;
    }

    private bool WaitTimeout(TimeSpan timeout)
    {
        if (Volatile.Read(ref _flag)) 
        {
            return true;
        }
        var deadline = Stopwatch.GetTimestamp() + Monotonic.Ticks(timeout);
        while (true)
        {
            var remainingTicks = deadline - Stopwatch.GetTimestamp();
            if (remainingTicks <= 0)
            {
                // Timeout reached, but notify could have raced
                return Volatile.Read(ref _flag);
            }
            var signaled = Monitor.Wait(_lock, Monotonic.FromTicks(remainingTicks));
            if (!signaled) // timoeut
            {
                // Timeout reached, but notify could have raced
                return Volatile.Read(ref _flag);
            }
            // There is a chance that `Condition.Notify` was called.
            if (Volatile.Read(ref _flag)) return true; // normal signal
            // spurious signal
        }
    }

    // Must be called under the lock, i.e. after `Condition.Acquire`
    public void NotifyOne()
    {
        Volatile.Write(ref _flag, true);
        Monitor.Pulse(_lock);
    }

    // Must be called under the lock. i.e. after `Condition.Acquire`
    public void NotifyAll()
    {
        Volatile.Write(ref _flag, true);
        Monitor.PulseAll(_lock);
    }

    public void Release()
    {
        Monitor.Exit(_lock);
    }

    internal void SpuriousSignal()
    {
        Monitor.PulseAll(_lock);
    }
}
