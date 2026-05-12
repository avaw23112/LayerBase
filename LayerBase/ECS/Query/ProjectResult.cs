namespace LayerBase.ECS;

/// <summary>
/// [Query] + [Bring] 方法的返回值类型。
/// 控制 TouchProjectedActor 和 Post 行为。
/// </summary>
public enum ProjectResult : byte
{
    /// <summary>
    /// 不 Touch，不 Post。
    /// 适合 AOI 外或条件不满足时跳过。
    /// </summary>
    Fail = 0,

    /// <summary>
    /// TouchProjectedActor，不 Post。
    /// 适合 AOI 内保活但本帧无行为事件。
    /// </summary>
    Touch = 1,

    /// <summary>
    /// TouchProjectedActor，并 Post Bring 事件。
    /// </summary>
    Success = 2
}
