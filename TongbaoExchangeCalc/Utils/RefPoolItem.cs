using System;
using System.Collections.Generic;

public abstract class RefPoolItem : IPoolItem
{
    public int RefCount { get; private set; }
    private bool mIsInPool;

    public event Action OnRefCountZero;

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
        if (mIsInPool)
        {
            return;
        }

        OnRefCountZero?.Invoke();
    }

    public void OnAllocate()
    {
        RefCount = 1;
        mIsInPool = false;
    }

    public void OnRecycle()
    {
        if (RefCount > 0)
        {
            throw new InvalidOperationException("Cannot recycle object while RefCount > 0.");
        }

        RefCount = 0;
        mIsInPool = true;
    }
}
