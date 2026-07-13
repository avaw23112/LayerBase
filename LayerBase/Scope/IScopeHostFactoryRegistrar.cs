using System.Collections.Generic;
using LayerBase.Actor;
using LayerBase.DI;

namespace LayerBase.Scope;

public delegate ScopeRuntimeHost? ScopeHostFactoryDelegate(
    IReadOnlyList<IService> services,
    ScopeRuntimeOptions? options,
    ActorWorld? sharedActorWorld,
    LayerRuntime? owningRuntime);

public interface IScopeHostFactoryRegistrar
{
    ScopeHostFactoryDelegate CreateScopeHostFactory();
}
