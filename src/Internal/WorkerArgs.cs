using System.Collections.Concurrent;

namespace Futures.Internal;

internal sealed class WorkerArgs
{
    public WorkerArgs(ThreadPoolExecutor executor, ConcurrentQueue<Action?> queue)
    {
        PoolRef = new WeakReference<ThreadPoolExecutor>(executor);
        Queue = queue;
    }

    public WeakReference<ThreadPoolExecutor> PoolRef { get; }
    public ConcurrentQueue<Action?> Queue { get; }

    public void Deconstruct(out WeakReference<ThreadPoolExecutor> poolRef, out ConcurrentQueue<Action?> queue)
    {
        poolRef = PoolRef;
        queue = Queue;
    }
}
