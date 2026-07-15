using Arch.Core;

namespace LayerBase.ECS;

internal interface IEcsScheduler : IDisposable
{
    World World { get; }

    EcsQueryBatchOptions BatchOptions { get; }

    void BeginTick();

    void FlushStructuralChanges();

    void EndTick();

    void Stop();
}
