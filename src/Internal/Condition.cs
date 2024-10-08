﻿namespace Futures.Internal;

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
        if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }
        if (timeout == Timeout.InfiniteTimeSpan)
        {
            return WaitInfinite();
        }
        return WaitTimeout(timeout);
    }

    private bool WaitInfinite()
    {
        while (!Volatile.Read(ref _flag))
        {
            var signaled = Monitor.Wait(_lock);
            if (!signaled) return false;
        }
        return true;
    }

    private bool WaitTimeout(TimeSpan timeout)
    {
        if (Volatile.Read(ref _flag)) 
        {
            return true;
        }
        var until = DateTime.UtcNow.Add(timeout).Ticks;
        long duration = timeout.Ticks;
        do
        {
            var signaled = Monitor.Wait(_lock, TimeSpan.FromTicks(duration));
            if (Volatile.Read(ref _flag)) return true; // valid signal
            if (!signaled) return false; // timeout
            var delta = until - DateTime.UtcNow.Ticks; // spurious signal
            if (delta <= 0) return false;
        }
        while (true);
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
