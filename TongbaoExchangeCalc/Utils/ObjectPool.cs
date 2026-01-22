using System;
using System.Collections.Generic;

public interface IPoolItem
{
    void OnAllocate();
    void OnRecycle();
}

/// <summary>
/// 对象池
/// </summary>
/// <typeparam name="T"></typeparam>
public class ObjectPool<T>
{
    /// <summary>
    /// 缓存列表
    /// </summary>
    private Stack<T> mCacheStack;
    /// <summary>
    /// 工厂创建函数
    /// </summary>
    private Func<T> mFactoryFunc;
    /// <summary>
    /// 最大缓存个数
    /// </summary>
    public int CacheMaxCount = 0;
    /// <summary>
    /// 默认的初始化函数
    /// </summary>
    public const int DefaultInitCount = 0;
    /// <summary>
    /// 默认的最大数量
    /// </summary>
    public const int DefaultMaxCount = 32;

    public bool? _isValueType;
    public bool IsValueType //是否为值类型
    {
        get
        {
            _isValueType ??= typeof(T).IsValueType;
            return _isValueType.Value;
        }
    }

    /// <summary>
    /// 不带创建函数，如果池里没有数据，则返回null
    /// </summary>
    /// <param name="initSize">初始个数</param>
    /// <param name="cacheMaxCount">最大缓存个数</param>
    public ObjectPool(int initSize = DefaultInitCount, int cacheMaxCount = DefaultMaxCount)
    {
        CacheMaxCount = cacheMaxCount;
        mCacheStack = new Stack<T>(CacheMaxCount);
        PreCreate(initSize);
    }

    /// <summary>
    /// 带创建函数，如果池里没有数据，则会创建
    /// </summary>
    /// <param name="createFunc">创建函数</param>
    /// <param name="initSize">初始个数</param>
    /// <param name="cacheMaxCount">最大缓存个数</param>
    public ObjectPool(Func<T> createFunc, int initSize = DefaultInitCount, int cacheMaxCount = DefaultMaxCount)
    {
        CacheMaxCount = cacheMaxCount;
        mFactoryFunc = createFunc;
        mCacheStack = new Stack<T>(CacheMaxCount);
        PreCreate(initSize);
    }

    /// <summary>
    /// 分配
    /// </summary>
    /// <returns></returns>
    public T Allocate()
    {
        T item;
        if (mCacheStack.Count <= 0)
        {
            item = CreateItem();
        }
        else
        {
            item = mCacheStack.Pop();
        }
        if (item is IPoolItem poolItem)
        {
            poolItem.OnAllocate();
        }
        return item;
    }

    /// <summary>
    /// 回收
    /// </summary>
    /// <param name="item"></param>
    /// <param name="notCheckContains">当能保证不会重复的情况下，可以设为true，节省Contains所需的性能，用于每帧回收的情况</param>
    public void Recycle(T item, bool notCheckContains = false)
    {
        int count = mCacheStack.Count;
        if (count >= CacheMaxCount)
        {
            CacheMaxCount *= 2;
            System.Diagnostics.Debug.WriteLine($"Cache expand: {CacheMaxCount}   {typeof(T).FullName}");
        }

        if (count < CacheMaxCount)
        {
            if (IsValueType || (notCheckContains || !Contains(item)))
            {
                if (item is IPoolItem poolItem)
                {
                    poolItem.OnRecycle();
                }
                mCacheStack.Push(item);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Pool Has Item: " + item + "   " + typeof(T).FullName);
            }
        }
    }

    /// <summary>
    /// 是否包含对象
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Contains(T item)
    {
        return mCacheStack.Contains(item);
    }

    /// <summary>
    /// 是否满了
    /// </summary>
    /// <returns></returns>
    public bool IsFull()
    {
        return mCacheStack.Count >= CacheMaxCount;
    }

    /// <summary>
    /// 是否为空
    /// </summary>
    /// <returns></returns>
    public bool IsEmpty()
    {
        return mCacheStack.Count <= 0;
    }

    /// <summary>
    /// 预创建
    /// </summary>
    private void PreCreate(int initSize)
    {
        for (int i = 0; i < initSize; i++)
        {
            T item = CreateItem();
            Recycle(item);
        }
    }

    /// <summary>
    /// 创建一个Item
    /// </summary>
    /// <returns></returns>
    private T CreateItem()
    {
        if (mFactoryFunc != null)
        {
            T Item = mFactoryFunc.Invoke();
            return Item;
        }
        else
        {
            return (T)Activator.CreateInstance(typeof(T));
        }
    }

    public void ReleasePool()
    {
        mCacheStack?.Clear();
        mCacheStack = null;
        mFactoryFunc = null;
    }
}
