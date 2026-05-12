using System;
using System.Collections.Concurrent;

namespace LayerBase.Async;

public readonly struct MainThreadCompletionItem
{
    private readonly Action _complete;

    public MainThreadCompletionItem(Action complete)
    {
        _complete = complete ?? throw new ArgumentNullException(nameof(complete));
    }

    public void Complete()
    {
        _complete();
    }
}

public sealed class MainThreadCompletionQueue
{
    private readonly ConcurrentQueue<MainThreadCompletionItem> _queue = new();

    public void Enqueue(MainThreadCompletionItem item)
    {
        _queue.Enqueue(item);
    }

    public void Enqueue(Action action)
    {
        _queue.Enqueue(new MainThreadCompletionItem(action));
    }

    public CompletionDrainStats Drain(
        int                       maxCount,
        CompletionExceptionPolicy exceptionPolicy,
        Action<Exception>?        reportException)
    {
        var processed = 0;
        var errors = 0;

        while ((maxCount <= 0 || processed < maxCount) &&
               _queue.TryDequeue(out var item))
        {
            try
            {
                item.Complete();
                processed++;
            }
            catch (Exception ex)
            {
                errors++;
                if (exceptionPolicy == CompletionExceptionPolicy.Throw)
                {
                    throw;
                }

                reportException?.Invoke(ex);
                processed++; // Count as processed even if it failed
            }
        }

        return new CompletionDrainStats(processed - errors, errors, _queue.Count);
    }
}