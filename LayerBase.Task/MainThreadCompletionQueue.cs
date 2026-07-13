using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace LayerBase.Async;

public readonly struct MainThreadCompletionItem
{
    private readonly Action _complete;
    private readonly Action<Exception>? _cancelOnClose;

    public MainThreadCompletionItem(Action complete, Action<Exception>? cancelOnClose = null)
    {
        _complete = complete ?? throw new ArgumentNullException(nameof(complete));
        _cancelOnClose = cancelOnClose;
    }

    public void Complete()
    {
        _complete();
    }

    public void CancelOnClose(Exception error)
    {
        _cancelOnClose?.Invoke(error);
    }
}

public sealed class MainThreadCompletionQueue
{
    private readonly ConcurrentQueue<MainThreadCompletionItem> _queue = new();
    private readonly object _gate = new();
    private ObjectDisposedException? _closed;

    public void Enqueue(MainThreadCompletionItem item)
    {
        lock (_gate)
        {
            ThrowIfClosed();
            _queue.Enqueue(item);
        }
    }

    public void Enqueue(Action action)
    {
        Enqueue(new MainThreadCompletionItem(action));
    }

    public void Enqueue(Action action, Action<Exception> cancelOnClose)
    {
        if (cancelOnClose == null) throw new ArgumentNullException(nameof(cancelOnClose));
        Enqueue(new MainThreadCompletionItem(action, cancelOnClose));
    }

    public void Close(ObjectDisposedException error)
    {
        if (error == null) throw new ArgumentNullException(nameof(error));

        MainThreadCompletionItem[] pending = CloseAndDetach(error);

        for (int i = 0; i < pending.Length; i++)
        {
            pending[i].CancelOnClose(error);
        }
    }

    public MainThreadCompletionItem[] CloseAndDetach(ObjectDisposedException error)
    {
        if (error == null) throw new ArgumentNullException(nameof(error));

        lock (_gate)
        {
            _closed ??= error;
            if (_queue.IsEmpty)
            {
                return Array.Empty<MainThreadCompletionItem>();
            }

            var pending = new List<MainThreadCompletionItem>();
            while (_queue.TryDequeue(out MainThreadCompletionItem item))
            {
                pending.Add(item);
            }

            return pending.ToArray();
        }
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

    private void ThrowIfClosed()
    {
        if (_closed != null)
        {
            throw _closed;
        }
    }
}
