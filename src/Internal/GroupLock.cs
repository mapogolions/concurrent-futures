namespace Futures.Internal;

internal sealed class GroupLock : ILockable
{
    private readonly IEnumerable<ILockable> _items;

    public GroupLock(params ILockable[] items)
    {
        _items = items;
    }

    public void Acquire()
    {
        foreach (var item in _items)
        {
            item.Acquire();
        }
    }


    public void Release()
    {
        foreach (var item in _items)
        {
            item.Release();
        }
    }
}
