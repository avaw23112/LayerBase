using Arch.Core;

namespace LayerBase.ECS.Runtime;

internal interface IEcsWorkItem
{
    string DebugName { get; }

    void Execute(World world, EcsResultQueue results);
}
