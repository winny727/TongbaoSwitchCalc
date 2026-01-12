using System;
using System.Diagnostics;

public sealed class CodeTimer : IDisposable
{
    private readonly Stopwatch mStopWatch;
    private readonly string mName;

    public CodeTimer(string name = "")
    {
        mName = string.IsNullOrEmpty(name) ? "CodeTimer" : name;
        mStopWatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        mStopWatch.Stop();
        //Console.WriteLine($"{mName} 耗时: {mStopWatch.Elapsed.TotalMilliseconds:F3} ms");
        Debug.WriteLine($"{mName} 耗时: {mStopWatch.Elapsed.TotalMilliseconds:F3} ms");
    }

    public float ElapsedMilliseconds => (float)mStopWatch.Elapsed.TotalMilliseconds;
}