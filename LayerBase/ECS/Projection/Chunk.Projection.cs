using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;
using LayerBase.ECS.Projection;

namespace Arch.Core;

public partial struct Chunk
{
    internal ProjectedActorMeta[] ProjectedActors { get; private set; } = Array.Empty<ProjectedActorMeta>();

    internal void InitializeProjectionStorage(
        int capacity)
    {
        ProjectedActors = new ProjectedActorMeta[capacity];
        for (int i = 0; i < ProjectedActors.Length; i++)
        {
            ProjectedActors[i] = ProjectedActorMeta.None;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref ProjectedActorMeta ProjectionAt(
        int row)
    {
        return ref ProjectedActors.DangerousGetReferenceAt(row);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref ProjectedActorMeta FirstProjection()
    {
        return ref ProjectedActors.DangerousGetReference();
    }
}