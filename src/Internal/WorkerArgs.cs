using System.Collections.Concurrent;

namespace Futures.Internal;

internal sealed class WorkerArgs
{
    public WorkerArgs(ThreadPoolExecutor executor, BlockingCollection<Action?> queue)
    {
        PoolRef = new WeakReference<ThreadPoolExecutor>(executor);
        Queue = queue;
    }

    public WeakReference<ThreadPoolExecutor> PoolRef { get; }
    public BlockingCollection<Action?> Queue { get; }

    public void Deconstruct(out WeakReference<ThreadPoolExecutor> poolRef, out BlockingCollection<Action?> queue)
    {
        poolRef = PoolRef;
        queue = Queue;
    }
}
