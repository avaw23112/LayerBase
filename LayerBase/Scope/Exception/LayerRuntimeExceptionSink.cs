namespace LayerBase.Scope;

internal sealed class LayerRuntimeExceptionSink : ILayerExceptionSink
{
    private readonly LayerRuntime _runtime;
    private readonly LayerHubExceptionCallbacks _detailedCallbacks;

    public LayerRuntimeExceptionSink(
        LayerRuntime runtime,
        LayerHubExceptionCallbacks detailedCallbacks)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _detailedCallbacks = detailedCallbacks ?? throw new ArgumentNullException(nameof(detailedCallbacks));
    }

    public void OnException(in LayerExceptionRecord record)
    {
        DispatchDetailedRecord(in record);
        LayerEventInfo legacyInfo = LayerExceptionInfoAdapter.ToLayerEventInfo(_runtime, in record);
        _runtime.ReportInfo(legacyInfo);
    }

    public void OnExceptionQueueOverflow(int droppedCount, in LayerExceptionRecord lastRecord)
    {
        DispatchDetailedOverflow(droppedCount, in lastRecord);

        var overflowException = new LayerBaseQueueOverflowException(
            lastRecord.ScopeId,
            LayerQueueKind.ExceptionQueue,
            lastRecord.QueueCapacity,
            droppedCount);

        var overflowRecord = new LayerExceptionRecord(
            exception: overflowException,
            scopeId: lastRecord.ScopeId,
            serviceId: lastRecord.ServiceId,
            phase: LayerExceptionPhase.QueueOverflow,
            queueKind: LayerQueueKind.ExceptionQueue,
            messageId: lastRecord.MessageId,
            trace: lastRecord.Trace,
            threadId: Environment.CurrentManagedThreadId,
            tick: lastRecord.Tick,
            queueCapacity: lastRecord.QueueCapacity,
            queueCount: droppedCount,
            layerIndex: lastRecord.LayerIndex,
            source: lastRecord.Source,
            eventName: lastRecord.EventName);

        LayerEventInfo legacyInfo = LayerExceptionInfoAdapter.ToLayerEventInfo(_runtime, in overflowRecord);
        _runtime.ReportInfo(legacyInfo);
    }

    private void DispatchDetailedRecord(in LayerExceptionRecord record)
    {
        try
        {
            ((ILayerExceptionSink)_detailedCallbacks).OnException(in record);
        }
        catch (Exception exception)
        {
            LayerHub.ReportEmergencyCallbackFailure(exception);
        }
    }

    private void DispatchDetailedOverflow(int droppedCount, in LayerExceptionRecord record)
    {
        try
        {
            ((ILayerExceptionSink)_detailedCallbacks).OnExceptionQueueOverflow(droppedCount, in record);
        }
        catch (Exception exception)
        {
            LayerHub.ReportEmergencyCallbackFailure(exception);
        }
    }
}
