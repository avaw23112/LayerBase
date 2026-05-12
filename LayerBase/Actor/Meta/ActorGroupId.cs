namespace LayerBase.Actor;

internal static class ActorGroupId<TGroup>
    where TGroup : struct, IActorGroup
{
    public static readonly int Id = ActorGroupIdAllocator.GetOrCreate(typeof(TGroup));
}