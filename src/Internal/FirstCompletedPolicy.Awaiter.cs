namespace Futures.Internal;

internal sealed partial class FirstCompletedPolicy<T>
{
    private sealed class FirstCompletedAwaiter : IFutureAwaiter<T>
    {
        private readonly object _lock = new();
        private readonly List<Future<T>> _completed = new();
        private readonly FirstCompletedPolicy<T> _policy;

        public FirstCompletedAwaiter(FirstCompletedPolicy<T> awaiter)
        {
            _policy = awaiter ?? throw new ArgumentNullException(nameof(awaiter));
            foreach (var future in awaiter._futures)
            {
                future.AddPolicy(this);
            }
        }

        public IReadOnlyCollection<Future<T>> Done()
        {
            foreach (var future in _policy._futures)
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

        public void Add(Future<T> future)
        {
            lock(_lock)
            {
                _completed.Add(future);
                _policy._event.Set();
            }
        }
    }
}
