using Arch.Core;
using LayerBase.ECS.Projection;
using LayerBase.ECS.Projection.Generated;

namespace LayerBase;

public sealed partial class LayerRuntime
{
    public World EcsWorld => _scopeHost.MainScope.EcsWorld;
}
