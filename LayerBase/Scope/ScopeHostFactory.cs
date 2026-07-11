using System;
using System.Collections.Generic;
using LayerBase.Actor;
using LayerBase.DI;

namespace LayerBase.Scope;

/// <summary>
/// 由源生成器在编译期填充的 ScopeRuntimeHost 工厂注册表。
/// 替代原先的运行时反射方案，通过 Layer is 检测注册，IL2CPP/AOT 友好。
/// </summary>
public static class ScopeHostFactory
{
    public delegate ScopeRuntimeHost? FactoryDelegate(
        IReadOnlyList<IService> services,
        ScopeRuntimeOptions? options,
        ActorWorld? sharedActorWorld,
        LayerRuntime? owningRuntime);

    private static FactoryDelegate? s_factory;

    public static void Register(FactoryDelegate factory)
    {
        s_factory = factory;
    }

    public static ScopeRuntimeHost? TryCreate(
        IReadOnlyList<IService> services,
        ScopeRuntimeOptions? options = null,
        ActorWorld? sharedActorWorld = null,
        LayerRuntime? owningRuntime = null)
    {
        return s_factory?.Invoke(services, options, sharedActorWorld, owningRuntime);
    }
}
