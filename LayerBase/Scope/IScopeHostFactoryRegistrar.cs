namespace LayerBase.Scope;

/// <summary>
/// 由源生成器在含有 Scope 类型的项目中实现到 partial Layer 上。
/// 在 Build 阶段通过 is 检测发现，将生成的 ScopeRuntimeHostFactory 注册到 ScopeHostFactory。
/// 替代 ModuleInitializer（Unity IL2CPP 不兼容）和 Assembly 扫描反射方案。
/// </summary>
public interface IScopeHostFactoryRegistrar
{
    void RegisterScopeHostFactory();
}
