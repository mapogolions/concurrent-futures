using System.Collections.Concurrent;
using Futures.Internal;

namespace Futures;

public class ThreadPoolExecutor
{
    private readonly int _maxWorkers;
    private bool _shutdown;
    private readonly object _shutdownLock = new();
    private readonly BlockingCollection<Action?> _queue = new();
    private readonly SemaphoreSlim _sem = new(0);
    private readonly Thread[] _threads;

    public ThreadPoolExecutor() : this(Environment.ProcessorCount) { }

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
            _queue.Add(item.Run);
            this.Spawn();
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

    private void Spawn()
    {
        if (_sem.Wait(TimeSpan.Zero)) return;
        var size = _threads.Length;
        if (size >= _maxWorkers) return;
        var t = new Thread(Worker) { IsBackground = true };
        t.Start(new WorkerArgs(this, _queue));
        _threads[size] = t;
    }

    private static void Worker(object? state)
    {
        var (poolRef, queue) = (WorkerArgs)state!;
        ThreadPoolExecutor? executor = null;
        while (true)
        {
            if (queue.TryTake(out var action))
            {
                if (action is not null)
                {
                    action();
                    continue;
                }
            }
            else
            {
                // We are here because queue is empty
                if (poolRef.TryGetTarget(out executor))
                {
                    // We try to signal executor that the current thread is not busy
                    // This helps prevent new threads from being created too early.
                    executor._sem.Release();
                    executor = null;
                }
                // block until a new element is pushed onto the queue
                action = queue.Take();
                if (action is not null)
                {
                    action();
                    continue;
                }
            }

            if (!poolRef.TryGetTarget(out executor) || executor._shutdown)
            {
                queue.Add(null);
                return;
            }
        }
    }
}
