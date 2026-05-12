namespace LayerBase;

/// <summary>
/// Runtime 停止回调。
///
/// 它发生在 Runtime Dispose 释放服务之前。
/// 适合保存临时状态、取消外部订阅、清理非托管资源引用。
/// </summary>
public interface IRuntimeStop
{
    void RuntimeStop();
}