namespace LayerBase.Event.Delay
{
    /// <summary>
    /// 延迟事件的预期传播方向。
    /// </summary>
    public enum DelayDirection
    {
        None = 0,
        BroadCast,
        Bubble,
        Drop
    }
}
