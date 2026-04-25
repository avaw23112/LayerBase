namespace LayerBase.Async;

/// <summary>Manual completion for ArchTask.</summary>
public sealed class LBTaskCompletionSource : IDisposable
{
    private readonly ArchTaskSource _source;
    private int _disposed;

    public LBTaskCompletionSource()
    {
        _source = ArchTaskSource.Rent();
    }

    public LBTask Task => new(_source);

    public void Dispose()
    {
        DisposeInternal();
        GC.SuppressFinalize(this);
    }

    ~LBTaskCompletionSource()
    {
        DisposeInternal();
    }

    public void SetResult()
    {
        _source.SetResult();
    }

    public void SetException(Exception ex)
    {
        _source.SetException(ex);
    }

    public void SetCanceled(CancellationToken token = default)
    {
        _source.SetCanceled(token);
    }

    public bool TrySetResult()
    {
        try
        {
            _source.SetResult();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TrySetException(Exception ex)
    {
        try
        {
            _source.SetException(ex);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TrySetCanceled(CancellationToken token = default)
    {
        try
        {
            _source.SetCanceled(token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void DisposeInternal()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            // 如果任务从未完成，强制归�?Source 到池�?
            _source.TryRelease();
    }
}

/// <summary>Manual completion for ArchTask{T}.</summary>
public sealed class LBTaskCompletionSource<T> : IDisposable
{
    private readonly ArchTaskSource<T> _source;
    private int _disposed;

    public LBTaskCompletionSource()
    {
        _source = ArchTaskSource<T>.Rent();
    }

    public LBTask<T> Task => new(_source);

    public void Dispose()
    {
        DisposeInternal();
        GC.SuppressFinalize(this);
    }

    ~LBTaskCompletionSource()
    {
        DisposeInternal();
    }

    public void SetResult(T value)
    {
        _source.SetResult(value);
    }

    public void SetException(Exception ex)
    {
        _source.SetException(ex);
    }

    public void SetCanceled(CancellationToken token = default)
    {
        _source.SetCanceled(token);
    }

    public bool TrySetResult(T value)
    {
        try
        {
            _source.SetResult(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TrySetException(Exception ex)
    {
        try
        {
            _source.SetException(ex);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TrySetCanceled(CancellationToken token = default)
    {
        try
        {
            _source.SetCanceled(token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void DisposeInternal()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0) _source.TryRelease();
    }
}

