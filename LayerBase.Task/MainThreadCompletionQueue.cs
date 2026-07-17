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
    private readonly Action? _onWorkAvailable;
    private int _hasItems;

    public MainThreadCompletionQueue(
        Action? onWorkAvailable = null)
    {
        _onWorkAvailable = onWorkAvailable;
    }

    public int Count => _queue.Count;

    public bool HasPending =>
        Volatile.Read(ref _hasItems) != 0;

    public void Enqueue(MainThreadCompletionItem item)
    {
        _queue.Enqueue(item);
        Volatile.Write(ref _hasItems, 1);
        _onWorkAvailable?.Invoke();
    }

    public void Enqueue(Action action)
    {
        _queue.Enqueue(new MainThreadCompletionItem(action));
        Volatile.Write(ref _hasItems, 1);
        _onWorkAvailable?.Invoke();
    }

    public CompletionDrainStats Drain(
        int                       maxCount,
        CompletionExceptionPolicy exceptionPolicy,
        Action<Exception>?        reportException)
    {
        if (Interlocked.Exchange(ref _hasItems, 0) == 0)
            return new CompletionDrainStats(0, 0, 0);

        var processed = 0;
        var errors = 0;

        try
        {
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
                    processed++;
                }
            }
        }
        finally
        {
            if (!_queue.IsEmpty)
                Volatile.Write(ref _hasItems, 1);
        }

        return new CompletionDrainStats(processed - errors, errors, _queue.Count);
    }
}
