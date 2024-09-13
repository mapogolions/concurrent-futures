namespace Futures.Internal;

// In python, `threading.Condition` handles spurious wakesups
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
        while (!Volatile.Read(ref _flag))
        {
            var notTimeout = Monitor.Wait(_lock, timeout);
            if (!notTimeout) return false;
        }
        return true;
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
}
