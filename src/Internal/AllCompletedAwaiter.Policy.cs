namespace Futures.Internal;

internal sealed partial class AllCompletedAwaiter<T> : IFutureAwaiter<T>
{
    private sealed class AllCompletedAwaiterPolicy : IFutureAwaiterPolicy<T>
    {
        private readonly object _lock = new();
        private readonly List<Future<T>> _completed = new();
        private readonly AllCompletedAwaiter<T> _awaiter;
        private readonly IReadOnlyCollection<ICompletableFuture<T>> _uncompleted;

        public AllCompletedAwaiterPolicy(
            AllCompletedAwaiter<T> awaiter,
            IReadOnlyCollection<ICompletableFuture<T>> uncompleted)
        {
            _awaiter = awaiter ?? throw new ArgumentNullException(nameof(awaiter));
            _uncompleted = uncompleted ?? throw new ArgumentNullException(nameof(uncompleted));
            foreach (var future in _uncompleted)
            {
                future.AddPolicy(this);
            }
        }

        public IReadOnlyCollection<Future<T>> Done()
        {
            foreach (var future in _uncompleted)
            {
                future.Acquire();
                future.RemovePolicy(this);
                future.Release();
            }
            return _completed;
        }

        public void AddResult(Future<T> future) => this.Add(future);
        public void AddException(Future<T> future) => this.Add(future);
        public void AddCancellation(Future<T> future) => this.Add(future);

        private void Add(Future<T> future)
        {
            lock(_lock)
            {
                _completed.Add(future);
                if (_completed.Count == _uncompleted.Count)
                {
                    _awaiter._event.Set();
                }
            }
        }
    }
}
