namespace LayerBase.DI;

/// <summary>
/// 由源生成器实现的 Service 级自动挂载接口。
///
/// 作用：
/// 当 IService 实现类中存在 [Mount] ILayerContext 字段 / 属性时，
/// 生成器会生成该接口实现，并在其中把这些 ILayerContext 自动注册进当前 Layer scope。
/// </summary>
public interface IAutoServiceMount
{
    /// <summary>
    /// 自动注册当前 IService 内通过 [Mount] 声明的 ILayerContext 依赖。
    /// </summary>
    /// <param name="services">
    /// 当前 Layer 的 IServiceCollection。
    /// 调用时已经处于当前 IService 的 registration scope 中。
    /// </param>
    void __AutoMountContexts(IServiceCollection services);
}