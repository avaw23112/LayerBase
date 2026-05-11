using Arch.Core;
using LayerBase.ECS.Projection;
using LayerBase.ECS.Projection.Generated;

namespace LayerBase;

public sealed partial class LayerRuntime
{
    public World EcsWorld { get; private set; } = null!;

    internal ProjectedActorTypeRegistry ProjectedActorTypes { get; private set; } = null!;

    internal void InitializeEcsWorld()
    {
        EcsWorld = World.Create();
        EcsWorld.BindRuntime(this);

        ProjectedActorTypes = new ProjectedActorTypeRegistry();
        GeneratedProjectedActorTypes.RegisterTo(ProjectedActorTypes);
    }
}
