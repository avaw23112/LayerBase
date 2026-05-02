namespace LayerBase.Event.Delay;

internal interface IDelayPublisherInternal
{
    /// <summary>
    /// 当前 publisher 在 DelayPublisherManager 中的注册 ID。
    ///
    /// -1 表示尚未注册，或者已经注销。
    /// Layer 释放 DelayPublisher 时，通过该 ID 让 DelayPublisherManager 删除对应引用。
    /// </summary>
    int PublisherId { get; }

    void ClearValue();
    void Deactivate();
    bool TryExpire(int valueVersion);
    void Reset();
    bool HasActiveDelays { get; }
}
