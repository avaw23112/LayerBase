using LayerBase.Actor;
using LayerBase.Scope;
using LayerBase.Tooling;

namespace LayerBase;

internal sealed class RuntimeKernel
{
    public RuntimeKernel(LayerRuntime owner)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));

        Actors = new ActorWorld(owner);
        Exceptions = new LayerExceptionHub();
        Tools = new LayerToolRegistry(owner);
    }

    public ActorWorld Actors { get; }

    public LayerExceptionHub Exceptions { get; }

    public LayerToolRegistry Tools { get; }

    public ScopeRuntimeHost? ScopeHost { get; set; }
}
