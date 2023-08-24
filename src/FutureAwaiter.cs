using Futures.Internal;

namespace Futures;

public static class FutureAwaiter
{
    public static IFutureAwaiter<T> UsePolicy<T>(FutureAwaiterPolicy policy, params Future<T>[] futures)
    {
        return policy switch
        {
            FutureAwaiterPolicy.FirstCompleted => new FirstCompletedAwaiter<T>(futures),
            FutureAwaiterPolicy.AllCompleted => new AllCompletedAwaiter<T>(futures),
            _ => throw new NotImplementedException()
        };
    }

    public static IFutureAwaiter<T> UseFirstCompledPolicy<T>(params Future<T>[] futures) => UsePolicy(FutureAwaiterPolicy.FirstCompleted, futures);
    public static IFutureAwaiter<T> UseAllCompledPolicy<T>(params Future<T>[] futures) => UsePolicy(FutureAwaiterPolicy.AllCompleted, futures);
}
