using System.Collections.Concurrent;

namespace Futures.Internal;

internal sealed class WorkerArgs(ThreadPoolExecutor executor, BlockingCollection<Action?> queue)
{
    public WeakReference ExecutorRef { get; } = new WeakReference(executor);
    public BlockingCollection<Action?> Queue { get; } = queue;

    public void Deconstruct(out WeakReference executorRef, out BlockingCollection<Action?> queue)
    {
        executorRef = ExecutorRef;
        queue = Queue;
    }
}
