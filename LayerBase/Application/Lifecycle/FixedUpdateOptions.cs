namespace LayerBase;

/// <summary>
/// 固定步长更新配置。
/// </summary>
public readonly struct FixedUpdateOptions
{
    /// <summary>
    /// 创建固定步长更新配置。
    /// </summary>
    /// <param name="enabled">
    /// 是否启用固定步长更新。
    /// false 表示 Runtime 不执行 IFixedUpdate。
    /// </param>
    /// <param name="fixedDeltaTime">
    /// 固定步长时间。
    /// 例如 1f / 60f。
    /// </param>
    /// <param name="maxStepsPerPump">
    /// 单次 Pump 最多执行多少次 FixedUpdate。
    /// 它用于避免 deltaTime 很大时一次补太多帧导致卡死。
    /// </param>
    public FixedUpdateOptions(
        bool  enabled,
        float fixedDeltaTime,
        int   maxStepsPerPump)
    {
        Enabled = enabled;
        FixedDeltaTime = fixedDeltaTime <= 0 ? 1f / 60f : fixedDeltaTime;
        MaxStepsPerPump = maxStepsPerPump <= 0 ? 4 : maxStepsPerPump;
    }

    public bool Enabled { get; }
    public float FixedDeltaTime { get; }
    public int MaxStepsPerPump { get; }

    public static FixedUpdateOptions Disabled => new(false, 1f / 60f, 4);

    public static FixedUpdateOptions Default => new(true, 1f / 60f, 4);
}