using System;
using System.Collections.Generic;
using LayerBase.DI;

namespace LayerBase.Scope;

/// <summary>
/// 由源生成器在编译期通过 [ModuleInitializer] 填充的 ScopeRuntimeHost 工厂注册表。
/// 替代原先的运行时反射扫描 Assembly 方案，IL2CPP/AOT 友好。
/// </summary>
public static class ScopeHostFactory
{
    private static Func<IReadOnlyList<IService>, ScopeRuntimeOptions?, ScopeRuntimeHost?>? s_factory;

    public static void Register(
        Func<IReadOnlyList<IService>, ScopeRuntimeOptions?, ScopeRuntimeHost?> factory)
    {
        s_factory = factory;
    }

    public static ScopeRuntimeHost? TryCreate(
        IReadOnlyList<IService> services,
        ScopeRuntimeOptions? options = null)
    {
        return s_factory?.Invoke(services, options);
    }
}
