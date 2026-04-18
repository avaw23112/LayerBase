namespace LayerBase.Event.Delay
{
    /// <summary>
    /// 延迟分发器：允许查看当前挂起的延迟事件。
    /// 用户只能读取或取出事件，不能直接修改发布状态（发布需通过 Send/Delay 系列 API）。
    /// </summary>
    public interface IDelayPublisher
    {
        bool HasValue { get; }
    }

    public interface IDelayPublisher<T> : IDelayPublisher where T : struct
    {
        /// <summary>
        /// 尝试获取当前挂起的数值（不消耗）。
        /// </summary>
        bool TryGet(out T value);

        /// <summary>
        /// 尝试取出当前挂起的数值（成功后将清除挂起状态）。
        /// </summary>
        bool TryTake(out T value);

        /// <summary>
        /// 当前挂起事件的预期传播方向。
        /// </summary>
        DelayDirection Direction { get; }

        /// <summary>
        /// 当前挂起事件的契约 ID。
        /// </summary>
        int ContractId { get; }
    }
}
