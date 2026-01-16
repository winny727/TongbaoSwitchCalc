using System;
using System.Collections.Generic;
using System.Diagnostics;

public sealed class CodeTimer : IDisposable
{
    private readonly Stopwatch mStopWatch;
    public string Name { get; private set; }
    public float ElapsedMilliseconds => (float)mStopWatch.Elapsed.TotalMilliseconds;

    private static readonly Stack<CodeTimer> mPool = new Stack<CodeTimer>();

    public static CodeTimer StartNew(string name = "")
    {
        if (mPool.Count > 0)
        {
            CodeTimer timer = mPool.Pop();
            timer.Name = name;
            timer.mStopWatch.Restart();
            return timer;
        }
        return new CodeTimer(name);
    }

    public CodeTimer(string name = "")
    {
        Name = string.IsNullOrEmpty(name) ? "CodeTimer" : name;
        mStopWatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        mStopWatch.Stop();
        if (!mPool.Contains(this))
        {
            mPool.Push(this);
        }
        //Console.WriteLine($"{mName} 耗时: {mStopWatch.Elapsed.TotalMilliseconds:F3} ms");
        Debug.WriteLine($"{Name} 耗时: {mStopWatch.Elapsed.TotalMilliseconds:F3} ms");
    }
}