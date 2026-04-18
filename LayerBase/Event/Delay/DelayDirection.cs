namespace LayerBase.Event.Delay
{
    /// <summary>
    /// 延迟事件的预期传播方向。
    /// </summary>
    public enum DelayDirection
    {
        None = 0,
        
        /// <summary> 仅当前层级 </summary>
        Local,
        
        /// <summary> 全局广播 </summary>
        BroadCast,
        
        /// <summary> 向上冒泡 </summary>
        Bubble,
        
        /// <summary> 向下下沉 </summary>
        Drop
    }
}
