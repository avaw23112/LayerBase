using System.Collections.Generic;
using LayerBase.Actor;
using LayerBase.DI;

namespace LayerBase.Scope;

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
