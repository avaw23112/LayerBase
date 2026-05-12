using LayerBase.Actor;

namespace LayerBase.ECS.Projection.Generated;

public static class ActorType<TActor>
    where TActor : class, IActor, new()
{
    public static readonly int Id = ProjectedActorAllocator.Allocate();
}

internal static class ProjectedActorAllocator
{
    private static int s_nextId;

    public static int Allocate()
    {
        return Interlocked.Increment(ref s_nextId);
    }

    public static int MaxId => Volatile.Read(ref s_nextId);
}