using System.Collections.Concurrent;
using Futures.Internal;

namespace Futures;

public class ThreadPoolExecutor<T>
{
    private readonly int _maxWorkers;
    private bool _shoutdown;
    private object _shoutdownLock = new();
    private readonly ConcurrentQueue<WorkItem<T>> _queue;
    private readonly SemaphoreSlim _sem = new(0);
    private readonly Thread[] _threads;

    public Future<T> Submit(Func<object?, T> callback, object? state)
    {
        lock(_shoutdownLock)
        {
            if (_shoutdown)
            {
                throw new InvalidOperationException("cannot schedule new future");
            }
            var future = new Future<T>();
            var item = new WorkItem<T>(future, callback, state);
            _queue.Enqueue(item);
            this.AdjustThreadsUnsafe();
            return future;
        }
    }

    private void AdjustThreadsUnsafe()
    {
        if (_sem.Wait(TimeSpan.Zero)) return;
        var size = _threads.Length;
        if (size >= _maxWorkers) return;
        var t = new Thread(() =>
        {
        }) { IsBackground = true };
        t.Start();
        _threads[size] = t;
    }
}
