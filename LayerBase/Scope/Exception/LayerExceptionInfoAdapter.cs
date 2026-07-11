namespace LayerBase.Scope;

internal static class LayerExceptionInfoAdapter
{
    public static LayerEventInfo ToLayerEventInfo(
        LayerRuntime runtime,
        in LayerExceptionRecord record)
    {
        string source = !string.IsNullOrEmpty(record.Source)
            ? record.Source!
            : CreateSource(in record);
        string eventName = !string.IsNullOrEmpty(record.EventName)
            ? record.EventName!
            : CreateEventName(in record);
        string message = CreateMessage(runtime, in record);

        return new LayerEventInfo(
            record.LayerIndex,
            source,
            eventName,
            message,
            LayerEventInfoType.Error,
            record.Exception);
    }

    private static string CreateSource(in LayerExceptionRecord record)
    {
        return record.ServiceId >= 0
            ? $"Scope[{record.ScopeId}]/Service[{record.ServiceId}]"
            : $"Scope[{record.ScopeId}]";
    }

    private static string CreateEventName(in LayerExceptionRecord record)
    {
        return record.MessageId >= 0
            ? $"{record.Phase}:{record.MessageId}"
            : record.Phase.ToString();
    }

    private static string CreateMessage(LayerRuntime runtime, in LayerExceptionRecord record)
    {
        return
            $"Runtime={runtime.Id}, " +
            $"Scope={record.ScopeId}, " +
            $"Service={record.ServiceId}, " +
            $"Phase={record.Phase}, " +
            $"Queue={record.QueueKind}, " +
            $"Thread={record.ThreadId}, " +
            $"Tick={record.Tick}, " +
            $"Trace={record.Trace.TraceId}, " +
            $"ParentTrace={record.Trace.ParentTraceId}: " +
            record.Exception.Message;
    }
}
