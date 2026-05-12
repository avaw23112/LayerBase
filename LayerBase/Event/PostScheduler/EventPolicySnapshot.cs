namespace LayerBase.Core.Event;

/// <summary>
/// 事件策略快照。
/// </summary>
public readonly struct EventPolicySnapshot
{
    /// <summary>
    /// 创建事件策略快照。
    /// </summary>
    /// <param name="runtimeId">
    /// 运行期事件 ID。
    /// </param>
    /// <param name="identity">
    /// 事件稳定身份。
    /// </param>
    /// <param name="postPolicy">
    /// Post 投递策略。
    /// </param>
    /// <param name="timerPolicy">
    /// Timer 策略。
    /// </param>
    /// <param name="bufferPolicy">
    /// Buffer 策略。
    /// </param>
    public EventPolicySnapshot(
        int                runtimeId,
        EventIdentity      identity,
        EventPostPolicy?   postPolicy,
        EventTimerPolicy?  timerPolicy,
        EventBufferPolicy? bufferPolicy)
    {
        RuntimeId = runtimeId;
        Identity = identity;
        PostPolicy = postPolicy;
        TimerPolicy = timerPolicy;
        BufferPolicy = bufferPolicy;
    }

    public int RuntimeId { get; }
    public EventIdentity Identity { get; }
    public EventPostPolicy? PostPolicy { get; }
    public EventTimerPolicy? TimerPolicy { get; }
    public EventBufferPolicy? BufferPolicy { get; }
}