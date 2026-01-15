using LayerBase.Core.Event;

namespace LayerBase.Core.EventStateTrace;

internal struct PathFrame
{
    public long Timestamp;
    public string LayerName;
    public int HandlerStart;
    public int HandlerCount;
}

internal struct HandlerVisit
{
    public string HandlerName;
    public EventHandledState State;
}

/// <summary>
/// 存储在池中的事件路径数据。
/// </summary>
internal struct EventPath
{
    public PathFrame[]? Frames;
    public HandlerVisit[]? Handlers;
    public int FrameCount;
    public int HandlerCount;
    public ushort Version;

    public bool HasValue => Frames != null && Handlers != null;
}
