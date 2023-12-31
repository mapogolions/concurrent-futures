namespace Futures.Internal;

internal sealed partial class AllCompletedPolicy<T>
{
    private sealed class AllCompletedAwaiter : IFutureAwaiter<T>
    {
        private readonly object _lock = new();
        private readonly List<Future<T>> _completed = new();
        private readonly AllCompletedPolicy<T> _policy;
        private readonly IReadOnlyCollection<ICompletableFuture<T>> _uncompleted;

        public AllCompletedAwaiter(
            AllCompletedPolicy<T> policy,
            IReadOnlyCollection<ICompletableFuture<T>> uncompleted)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _uncompleted = uncompleted ?? throw new ArgumentNullException(nameof(uncompleted));
            foreach (var future in _uncompleted)
            {
                future.Subscribe(this);
            }
        }

        public IReadOnlyCollection<Future<T>> Done()
        {
            foreach (var future in _uncompleted)
            {
                future.Acquire();
                future.Unsubscribe(this);
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
                    _policy._event.Set();
                }
            }
        }
    }
}
