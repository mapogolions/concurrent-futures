using System.Collections.Concurrent;
using Futures.Internal;

namespace Futures;

public class ThreadPoolExecutor
{
    private readonly int _maxWorkers;
    private bool _shutdown;
    private object _shutdownLock = new();
    private readonly ConcurrentQueue<Action> _queue = new();
    private readonly SemaphoreSlim _sem = new(0);
    private readonly Thread[] _threads;

    public ThreadPoolExecutor(int maxWorkers)
    {
        _maxWorkers = maxWorkers;
        _threads = new Thread[maxWorkers];
    }

    public Future<T> Submit<T>(Func<object?, T> callback, object? state)
    {
        lock(_shutdownLock)
        {
            if (_shutdown)
            {
                throw new InvalidOperationException("cannot schedule new future");
            }
            var future = new Future<T>();
            var item = new WorkItem<T>(future, callback, state);
            _queue.Enqueue(item.Run);
            this.AdjustThreadsUnsafe();
            return future;
        }
    }

    public void Shutdown(bool wait = true)
    {
        if (_shutdown) return;
        lock (_shutdownLock)
        {
            _shutdown = true;
            if (wait)
            {
                foreach (var t in _threads) t.Join();
            }
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
