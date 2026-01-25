using System.Diagnostics;
using System.Text;
using LayerBase.Core.Event;

namespace LayerBase.Core.EventStateTrace;

internal class EventLogTracer
{
    private readonly EventPathPool _pathPool;
    private volatile bool _enabled = true;

    internal EventLogTracer(int logQueueCapacity)
    {
        _pathPool = new EventPathPool();
        Logs = new EventTraceLogQueue();
    }

    public EventTraceLogQueue Logs { get; }

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public void Release(ref EventState eventState)
    {
        _pathPool.Return(eventState.PathHandle);
    }

    public void Register(ref EventState eventState)
    {
        eventState.PathHandle = _pathPool.Rent();
    }

    public bool TryBeginLayer(ref EventState eventState, string layerName, long timestamp = 0)
    {
        if (!_enabled) return false;

        if (string.IsNullOrEmpty(layerName)) return false;

        var path = eventState.PathHandle;
        if (!path.HasValue) return false;
        EnsureFrameCapacity(ref path, path.FrameCount + 1);

        var ts = timestamp != 0 ? timestamp : Stopwatch.GetTimestamp();
        var frameIndex = path.FrameCount;
        path.Frames![frameIndex] = new PathFrame
        {
            Timestamp = ts,
            LayerName = layerName,
            HandlerStart = path.HandlerCount,
            HandlerCount = 0
        };
        path.FrameCount = frameIndex + 1;

        eventState.PathHandle = path;
        return true;
    }

    public bool TryRecordHandler(ref EventState eventState, string handlerName, EventHandledState handledState)
    {
        if (!_enabled) return false;

        if (string.IsNullOrEmpty(handlerName)) return false;


        var path = eventState.PathHandle;
        if (!path.HasValue || path.FrameCount == 0) return false;

        EnsureHandlerCapacity(ref path, path.HandlerCount + 1);

        var handlerIndex = path.HandlerCount;
        path.Handlers![handlerIndex] = new HandlerVisit
        {
            HandlerName = handlerName,
            State = handledState
        };
        path.HandlerCount = handlerIndex + 1;

        var frameIndex = path.FrameCount - 1;
        ref var frame = ref path.Frames![frameIndex];
        frame.HandlerCount += 1;

        eventState.PathHandle = path;
        return true;
    }

    public bool TryExport(in EventState eventState, out string result)
    {
        return TryExportUnlocked(in eventState, out result);
    }

    private void EnsureFrameCapacity(ref EventPath path, int required)
    {
        if (path.Frames!.Length < required) path.Frames = _pathPool.GrowFrames(path.Frames, required);
    }

    private void EnsureHandlerCapacity(ref EventPath path, int required)
    {
        if (path.Handlers!.Length < required) path.Handlers = _pathPool.GrowHandlers(path.Handlers, required);
    }

    private bool TryExportUnlocked(in EventState state, out string result)
    {
        var path = state.PathHandle;
        if (!path.HasValue || path.FrameCount == 0)
        {
            result = string.Empty;
            return false;
        }

        var sb = new StringBuilder();
        var elapsedSeconds = (Stopwatch.GetTimestamp() - state.StartTimestamp) / (double)Stopwatch.Frequency;
        if (elapsedSeconds < 0) elapsedSeconds = 0;
        var startTime = DateTime.Now - TimeSpan.FromSeconds(elapsedSeconds);
        sb.Append("[Start=").Append(startTime.ToString("HH:mm")).Append(']');
        var nl = Environment.NewLine;
        for (var i = 0; i < path.FrameCount; i++)
        {
            sb.Append(nl);

            ref var frame = ref path.Frames![i];
            sb.Append('[').Append(frame.LayerName).Append(']');
            sb.Append('{');

            var start = frame.HandlerStart;
            var end = start + frame.HandlerCount;
            for (var h = start; h < end; h++)
            {
                if (h > start) sb.Append(" => ");

                var visit = path.Handlers![h];
                sb.Append(visit.HandlerName);
                sb.Append(" : ");
                sb.Append(visit.State);
            }

            sb.Append('}');
        }

        result = sb.ToString();
        return true;
    }

    public void Pump(ref EventState eventState)
    {
        var line = string.Empty;
        if (TryExportUnlocked(in eventState, out var exported)) line = exported;

        Release(ref eventState);

        if (!string.IsNullOrEmpty(line)) Logs.EnqueueOverwrite(line);
    }
}