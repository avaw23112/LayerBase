using LayerBase.Core.DataStruct;

namespace LayerBase.Scope;

/// <summary>
/// 统一异常通道。
/// 任意线程可 Report，由 MainScope / LayerRuntime 主 Pump 时 Drain。
/// </summary>
public sealed class LayerExceptionHub
{
    private readonly LockedBoundedRingQueue<LayerExceptionRecord> _queue;
    private readonly object _overflowGate = new();
    private int _overflowCount;
    private LayerExceptionRecord _lastOverflow;
    private bool _hasLastOverflow;

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

        lock (_overflowGate)
        {
            _overflowCount++;
            _lastOverflow = record;
            _hasLastOverflow = true;
        }
    }

    /// <summary>
    /// 排空异常队列并回调（建议只在 MainScope / LayerRuntime 主线程调用）。
    /// </summary>
    public void DrainAndDispatch(ILayerExceptionSink sink)
    {
        while (_queue.TryDequeue(out LayerExceptionRecord record))
        {
            try
            {
                sink.OnException(record);
            }
            catch (Exception exception)
            {
                LayerHub.ReportEmergencyCallbackFailure(exception);
            }
        }

        if (!TryTakeOverflow(out int overflow, out LayerExceptionRecord lastOverflow))
        {
            return;
        }

        try
        {
            sink.OnExceptionQueueOverflow(overflow, lastOverflow);
        }
        catch (Exception exception)
        {
            LayerHub.ReportEmergencyCallbackFailure(exception);
        }
    }

    private bool TryTakeOverflow(out int count, out LayerExceptionRecord record)
    {
        lock (_overflowGate)
        {
            if (!_hasLastOverflow)
            {
                count = 0;
                record = default;
                return false;
            }

            count = _overflowCount;
            record = _lastOverflow;
            _overflowCount = 0;
            _lastOverflow = default;
            _hasLastOverflow = false;
            return count > 0;
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
