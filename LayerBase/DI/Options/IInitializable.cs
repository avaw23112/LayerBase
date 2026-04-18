namespace LayerBase.DI.Options;

/// <summary>
///     统一初始化接口：在层级 Build 完成、依赖注入与事件订阅全部就绪后触发。
///     所有的业务启动逻辑（如初始分发）应在此执行，而不是在构造函数中。
/// </summary>
public interface IInitializable
{
    /// <summary>
    ///     执行初始化逻辑。此时该对象所属的 Layer 环境已完全就绪。
    /// </summary>
    void Initialize();
}