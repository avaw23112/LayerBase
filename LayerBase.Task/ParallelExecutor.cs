using System;
using System.Threading;

namespace LayerBase.Async;

internal sealed class ParallelExecutor
{
    public static readonly ParallelExecutor Instance = new();

    private int _activeTaskCount;
    
    /// <summary>
    /// Max background tasks allowed to be scheduled at the same time.
    /// </summary>
    public int MaxBackgroundTasks { get; set; } = 1024;

    public bool TrySchedule(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        if (Interlocked.Increment(ref _activeTaskCount) > MaxBackgroundTasks)
        {
            Interlocked.Decrement(ref _activeTaskCount);
            return false;
        }

        bool success = ThreadPool.QueueUserWorkItem(static state =>
        {
            var tuple = ((ParallelExecutor, Action))state!;
            try
            {
                tuple.Item2();
            }
            finally
            {
                Interlocked.Decrement(ref tuple.Item1._activeTaskCount);
            }
        }, (this, action));

        if (!success)
        {
            Interlocked.Decrement(ref _activeTaskCount);
        }

        return success;
    }
}
