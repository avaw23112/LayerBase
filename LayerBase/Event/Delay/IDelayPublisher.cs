namespace LayerBase.Event.Delay
{
    /// <summary>
    /// 延迟事件缓存的读取接口。
    /// </summary>
    public interface IDelayPublisher
    {
        bool HasValue { get; }
    }

    public interface IDelayPublisher<T> : IDelayPublisher where T : struct
    {
        /// <summary>尝试获取缓存的事件数据，成功后清空缓存。</summary>
        bool TryGet(out T value);

        /// <summary>最近一次写入的方向标记。</summary>
        DelayDirection Direction { get; }
    }
}
