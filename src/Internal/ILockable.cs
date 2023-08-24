namespace Futures.Internal;

internal interface ILockable
{
    void Acquire();
    void Release();
}
