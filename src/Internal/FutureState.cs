namespace Futures;

internal enum FutureState
{
    Pending,
    Running,
    Cancelled,
    CancellationPropagated,
    Finished
}
