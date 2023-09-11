using System.Collections.Concurrent;

namespace Futures.Internal;

internal sealed class WorkerArgs
{
    public WorkerArgs(ThreadPoolExecutor executor, BlockingCollection<Action?> queue)
    {
        ExecutorRef = new WeakReference(executor);
        Queue = queue;
    }

    public WeakReference ExecutorRef { get; }
    public BlockingCollection<Action?> Queue { get; }

    public void Deconstruct(out WeakReference executorRef, out BlockingCollection<Action?> queue)
    {
        executorRef = ExecutorRef;
        queue = Queue;
    }
}
