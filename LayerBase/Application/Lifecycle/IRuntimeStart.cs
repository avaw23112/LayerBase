namespace LayerBase;

/// <summary>
/// Runtime 启动回调。
///
/// 它发生在 Build 完成之后。
/// 它表示当前 Runtime 已经可以开始正常 Pump。
/// </summary>
public interface IRuntimeStart
{
    void RuntimeStart();
}
