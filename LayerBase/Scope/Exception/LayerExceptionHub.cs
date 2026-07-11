using LayerBase.Core.DataStruct;

namespace LayerBase.Scope;

/// <summary>
/// 统一异常通道。
/// 任意线程可 Report，由 MainScope / LayerRuntime 主 Pump 时 Drain。
/// </summary>
public sealed class LayerExceptionHub
{
    private readonly LockedBoundedRingQueue<LayerExceptionRecord> _queue;
    private int _overflowCount;
    private LayerExceptionRecord _lastOverflow;
    private int _hasLastOverflow;

    public LayerExceptionHub(int capacity = 512)
    {
        _queue = new LockedBoundedRingQueue<LayerExceptionRecord>(capacity);
    }

    /// <summary>
    /// 上报异常（任意线程安全）。只入队，不调用用户回调。
    /// </summary>
    public void Report(in LayerExceptionRecord record)
    {
        if (_queue.TryEnqueue(record))
        {
            return;
        }

        Interlocked.Increment(ref _overflowCount);
        _lastOverflow = record;
        Volatile.Write(ref _hasLastOverflow, 1);
    }

    /// <summary>
    /// 排空异常队列并回调（建议只在 MainScope / LayerRuntime 主线程调用）。
    /// </summary>
    public void DrainAndDispatch(ILayerExceptionSink sink)
    {
        while (_queue.TryDequeue(out LayerExceptionRecord record))
        {
            sink.OnException(record);
        }

        int overflow = Interlocked.Exchange(ref _overflowCount, 0);
        if (overflow <= 0)
        {
            return;
        }

        if (Volatile.Read(ref _hasLastOverflow) == 1)
        {
            sink.OnExceptionQueueOverflow(overflow, _lastOverflow);
            Volatile.Write(ref _hasLastOverflow, 0);
        }
    }
}

/// <summary>
/// 向后兼容的异常回调适配器，实现 ILayerExceptionSink。
/// 兼容旧的 LayerHub.OnException(event => ...) 模式。
/// </summary>
public sealed class LayerHubExceptionCallbacks : ILayerExceptionSink
{
    public event Action<LayerExceptionRecord>? OnExceptionRecord;
    public event Action<Exception>? OnException;
    public event Action<int, LayerExceptionRecord>? OnExceptionQueueOverflow;

    void ILayerExceptionSink.OnException(in LayerExceptionRecord record)
    {
        OnExceptionRecord?.Invoke(record);
        OnException?.Invoke(record.Exception);
    }

    void ILayerExceptionSink.OnExceptionQueueOverflow(int droppedCount, in LayerExceptionRecord lastRecord)
    {
        OnExceptionQueueOverflow?.Invoke(droppedCount, lastRecord);
    }
}
