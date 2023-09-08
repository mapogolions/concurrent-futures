using System.Collections.Concurrent;

namespace Futures.Internal;

internal sealed class WorkerArgs
{
    public WorkerArgs(ThreadPoolExecutor executor, BlockingCollection<Action?> queue)
    {
        ExecutorRef = new WeakReference<ThreadPoolExecutor>(executor);
        Queue = queue;
    }

    public WeakReference<ThreadPoolExecutor> ExecutorRef { get; }
    public BlockingCollection<Action?> Queue { get; }

    public void Deconstruct(out WeakReference<ThreadPoolExecutor> executorRef, out BlockingCollection<Action?> queue)
    {
        executorRef = ExecutorRef;
        queue = Queue;
    }
}
