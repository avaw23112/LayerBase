namespace LayerBase;

/// <summary>
/// 固定步长更新接口。
///
/// 固定步长更新不依赖具体游戏引擎。
/// 它适合需要稳定 tick 的系统，例如模拟、战斗结算、输入缓冲推进。
/// </summary>
public interface IFixedUpdate
{
    /// <summary>
    /// 执行固定步长更新。
    /// </summary>
    /// <param name="fixedDeltaTime">
    /// 固定步长时间。
    /// 例如 1f / 60f 表示每秒 60 次固定更新。
    /// </param>
    void FixedUpdate(float fixedDeltaTime);
}