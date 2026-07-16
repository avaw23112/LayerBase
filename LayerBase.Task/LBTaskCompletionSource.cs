namespace LayerBase.Async;

/// <summary>Manual completion for ArchTask.</summary>
public sealed class LBTaskCompletionSource : IDisposable
{
    private readonly LBTaskSource _source;
    private readonly int _version;
    private int _disposed;

    public LBTaskCompletionSource()
    {
        _source = LBTaskSource.Rent();
        _version = _source.Version;
    }

    public LBTask Task => new(_source, _version);

    public void Dispose()
    {
        DisposeInternal();
    }

    public void SetResult()
    {
        if (!TrySetResult())
            throw new InvalidOperationException("LBTask source is already completed or belongs to another lease.");
    }

    public void SetException(Exception ex)
    {
        if (!TrySetException(ex))
            throw new InvalidOperationException("LBTask source is already completed or belongs to another lease.");
    }

    public void SetCanceled(CancellationToken token = default)
    {
        if (!TrySetCanceled(token))
            throw new InvalidOperationException("LBTask source is already completed or belongs to another lease.");
    }

    public bool TrySetResult()
    {
        return _source.TrySetResult(_version);
    }

    public bool TrySetException(Exception ex)
    {
        return _source.TrySetException(_version, ex);
    }

    public bool TrySetCanceled(CancellationToken token = default)
    {
        return _source.TrySetCanceled(_version, token);
    }

    private void DisposeInternal()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            TrySetCanceled(default);
    }
}

/// <summary>Manual completion for ArchTask{T}.</summary>
public sealed class LBTaskCompletionSource<T> : IDisposable
{
    private readonly LBTaskSource<T> _source;
    private readonly int _version;
    private int _disposed;

    public LBTaskCompletionSource()
    {
        _source = LBTaskSource<T>.Rent();
        _version = _source.Version;
    }

    public LBTask<T> Task => new(_source, _version);

    public void Dispose()
    {
        DisposeInternal();
    }

    public void SetResult(T value)
    {
        if (!TrySetResult(value))
            throw new InvalidOperationException("LBTask source is already completed or belongs to another lease.");
    }

    public void SetException(Exception ex)
    {
        if (!TrySetException(ex))
            throw new InvalidOperationException("LBTask source is already completed or belongs to another lease.");
    }

    public void SetCanceled(CancellationToken token = default)
    {
        if (!TrySetCanceled(token))
            throw new InvalidOperationException("LBTask source is already completed or belongs to another lease.");
    }

    public bool TrySetResult(T value)
    {
        return _source.TrySetResult(_version, value);
    }

    public bool TrySetException(Exception ex)
    {
        return _source.TrySetException(_version, ex);
    }

    public bool TrySetCanceled(CancellationToken token = default)
    {
        return _source.TrySetCanceled(_version, token);
    }

    private void DisposeInternal()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            TrySetCanceled(default);
    }
}
