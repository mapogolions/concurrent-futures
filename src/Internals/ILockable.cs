namespace Futures.Internals;

internal interface ILockable
{
    void Acquire();
    void Release();
}
