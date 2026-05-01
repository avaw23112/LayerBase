using System;
using System.Collections.Concurrent;

namespace LayerBase.Async;

internal readonly struct MainThreadCompletionItem
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

internal sealed class MainThreadCompletionQueue
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

    public int Drain(int maxCount)
    {
        var count = 0;

        while ((maxCount <= 0 || count < maxCount) &&
               _queue.TryDequeue(out var item))
        {
            item.Complete();
            count++;
        }

        return count;
    }
}
