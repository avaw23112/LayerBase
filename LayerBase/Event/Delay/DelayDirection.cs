namespace LayerBase.Event.Delay;

/// <summary>
///     延迟事件的预期传播方向�?
/// </summary>
public enum DelayDirection
{
    None = 0,

    /// <summary> 仅当前层�?</summary>
    Local,

    /// <summary> 全局广播 </summary>
    BroadCast
}

