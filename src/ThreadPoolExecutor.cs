using System.Collections.Concurrent;
using Futures.Internal;

namespace Futures;

public class ThreadPoolExecutor : IDisposable
{
    private readonly int _maxWorkers;
    private bool _shutdown = false;
    private readonly object _shutdownLock = new();
    private readonly BlockingCollection<Action?> _queue = new();
    private readonly SemaphoreSlim _sem = new(0);
    private readonly Thread[] _threads;
    private int _spawns;

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

    public Future Submit(Func<object?, object> callback, object? state)
    {
        lock (_shutdownLock)
        {
            if (_shutdown)
            {
                throw new InvalidOperationException("cannot schedule new future");
            }
            var future = new Future();
            var item = new WorkItem<object>(future, callback, state);
            _queue.Add(item.Run);
            this.Spawn();
            return future;
        }
    }

    public void Shutdown(bool wait = true)
    {
        lock (_shutdownLock)
        {
            if (!_shutdown)
            {
                _shutdown = true;
                _queue.Add(null); // wakeup threads
            }
        }
        if (wait)
        {
            for (int i = 0; i < _spawns; i++) _threads[i].Join();
        }
    }

    private void Spawn()
    {
        if (_sem.Wait(TimeSpan.Zero)) return;
        if (_spawns >= _maxWorkers) return;
        var t = new Thread(Worker) { IsBackground = true };
        t.Start(new WorkerArgs(this, _queue));
        _threads[_spawns++] = t;
    }

    internal int SpawnedThreads => _spawns;

    private static void Worker(object? state)
    {
        var (executorRef, queue) = (WorkerArgs)state!;
        object? target = null;
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
                target = executorRef.Target;
                if (target is not null)
                {
                    // We try to signal executor that the current thread is not busy
                    // This helps prevent new threads from being created too early.
                    ((ThreadPoolExecutor)target)._sem.Release();
                    target = null;
                }
                // block until a new element is pushed onto the queue
                action = queue.Take();
                if (action is not null)
                {
                    action();
                    continue;
                }
            }
            target = executorRef.Target;
            if (target is null || ((ThreadPoolExecutor)target)._shutdown)
            {
                queue.Add(null);
                return;
            }
            target = null;
        }
    }

    public void Dispose()
    {
        this.Shutdown();
    }
}
