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
        var until = DateTime.UtcNow.Add(timeout).Ticks;
        long duration = timeout.Ticks;
        do
        {
            var signaled = Monitor.Wait(_lock, TimeSpan.FromTicks(duration));
            if (!signaled) // timeout
            {
                // There is a chance that `Condition.Notify` was called.
                return Volatile.Read(ref _flag);
            }
            if (Volatile.Read(ref _flag)) return true; // normal signal
            duration = until - DateTime.UtcNow.Ticks; // spurious signal
            if (duration <= 0)
            {
                // There is a chance that `Condition.Notify` was called.
                return Volatile.Read(ref _flag);
            }
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
