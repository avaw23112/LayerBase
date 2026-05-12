namespace LayerBase.Actor;

internal static class ActorTagId<TTag>
    where TTag : struct, IActorTag
{
    public static readonly int Id = ActorTagIdAllocator.GetOrCreate(typeof(TTag));
}