using System;
using System.Collections.Generic;

public abstract class RefPoolItem : IPoolItem<RefPoolItem>
{
    public int RefCount { get; private set; }
    public ObjectPool<RefPoolItem> OwnerPool { get; private set; }
    public bool IsInPool { get; private set; }

    public void Ref()
    {
        RefCount++;
    }

    public void UnRef()
    {
        if (RefCount <= 0)
        {
            throw new InvalidOperationException("UnRef called too many times");
        }

        RefCount--;

        if (RefCount <= 0)
        {
            TryRecycle();
        }
    }

    private void TryRecycle()
    {
        if (IsInPool)
        {
            return;
        }

        OwnerPool?.Recycle(this);
    }

    public void OnAllocate(ObjectPool<RefPoolItem> ownerPool)
    {
        RefCount = 1;
        IsInPool = false;
        OwnerPool = ownerPool;
    }

    public void OnRecycle(ObjectPool<RefPoolItem> ownerPool)
    {
        if (RefCount > 0)
        {
            throw new InvalidOperationException("Cannot recycle object while RefCount > 0.");
        }

        RefCount = 0;
        IsInPool = true;
        OwnerPool = null;
    }
}
